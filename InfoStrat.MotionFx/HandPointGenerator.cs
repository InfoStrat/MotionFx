using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using xn;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using Blake.NUI.WPF.Utility;
using System.Diagnostics;
using System.Windows.Media.Media3D;
using InfoStrat.MotionFx.ImageProcessing;
using DirectCanvas.Imaging;
using System.Windows.Threading;

namespace InfoStrat.MotionFx
{
    public class HandPointGenerator
    {
        bool isFirstPoseAttempted = false;

        #region Static Properties

        private static HandPointGenerator _default;
        public static HandPointGenerator Default
        {
            get
            {
                if (_default == null)
                    _default = new HandPointGenerator();
                return _default;
            }
        }

        #endregion

        #region Fields

        bool IsGeneratorThreadRunning = false;

        ImageProcessor imageProcessor;

        bool firstFrameNotified = false;

        Thread generationThread;

        Context context;
        DepthGenerator depthGenerator;

        private UserGenerator userGenerator;
        private SkeletonCapability skeletonCapability;
        private PoseDetectionCapability poseDetectionCapability;
        private string calibPose;
        private Dictionary<uint, Dictionary<SkeletonJoint, SkeletonJointPosition>> joints;

        private int[] histogram;

        Dictionary<int, HandSession> HandSessions = new Dictionary<int, HandSession>();

        private Color[] colors = { Colors.Red, Colors.Blue, Colors.ForestGreen, Colors.Yellow, Colors.Orange, Colors.Purple, Colors.White };
        private Color[] anticolors = { Colors.Green, Colors.Orange, Colors.Red, Colors.Purple, Colors.Blue, Colors.Yellow, Colors.Black };
        private int ncolors = 6;

        bool isFirstCalibrationComplete = false;

        #endregion

        #region Properties

        public ImageSource DepthMap
        {
            get
            {
                if (imageProcessor == null ||
                    imageProcessor.DepthPresenter == null)
                    return null;
                return imageProcessor.DepthPresenter.ImageSource;
            }
        }

        public ImageSource HandMap
        {
            get
            {
                if (imageProcessor == null ||
                    imageProcessor.HandPresenter == null)
                    return null;
                return imageProcessor.HandPresenter.ImageSource;
            }
        }

        #endregion

        #region Public Events

        #region FirstFrameReady

        public event EventHandler FirstFrameReady;

        private void OnFirstFrameReady()
        {
            if (FirstFrameReady == null)
                return;
            FirstFrameReady(null, EventArgs.Empty);
        }

        #endregion

        #region UserFound

        public event EventHandler<UserEventArgs> UserFound;

        protected void OnUserFound(int id)
        {
            if (UserFound == null)
                return;
            UserFound(null, new UserEventArgs(id));
        }

        #endregion

        #region UserLost

        public event EventHandler<UserEventArgs> UserLost;

        protected void OnUserLost(int id)
        {
            if (UserLost == null)
                return;
            UserLost(null, new UserEventArgs(id));
        }

        #endregion

        #region PoseRecognized

        public event EventHandler<UserEventArgs> PoseRecognized;

        protected void OnPoseRecognized(int id)
        {
            if (PoseRecognized == null)
                return;
            PoseRecognized(null, new UserEventArgs(id));
        }

        #endregion

        #region SkeletonReady

        public event EventHandler<UserEventArgs> SkeletonReady;

        protected void OnSkeletonReady(int id)
        {
            if (SkeletonReady == null)
                return;
            SkeletonReady(null, new UserEventArgs(id));
        }

        #endregion

        #region PointCreated

        public event EventHandler<HandPointEventArgs> PointCreated;

        private void OnPointCreated(int id, HandSession session)
        {
            if (PointCreated == null)
                return;
            PointCreated(null, new HandPointEventArgs(id, HandPointStatus.Down, session));
        }

        #endregion

        #region PointUpdated

        public event EventHandler<HandPointEventArgs> PointUpdated;

        private void OnPointUpdated(int id, HandSession session)
        {
            if (PointUpdated == null)
                return;
            PointUpdated(null, new HandPointEventArgs(id, HandPointStatus.Move, session));
        }

        #endregion

        #region PointDestroyed

        public event EventHandler<HandPointEventArgs> PointDestroyed;

        private void OnPointDestroyed(int id, HandSession session)
        {
            if (PointDestroyed == null)
                return;
            PointDestroyed(null, new HandPointEventArgs(id, HandPointStatus.Up, session));
        }

        #endregion

        #region FrameUpdated

        public event EventHandler FrameUpdated;

        private void OnFrameUpdated()
        {
            if (FrameUpdated == null)
                return;
            FrameUpdated(null, EventArgs.Empty);
        }

        #endregion

        #endregion

        #region Constructors

        private HandPointGenerator()
        {

        }

        #endregion

        #region Public Methods

        public void StartGenerating()
        {
            StartGenerating(Dispatcher.CurrentDispatcher);
        }

        public void StartGenerating(Dispatcher dispatcher)
        {
            DirectCanvas.Misc.Size imageSize = new DirectCanvas.Misc.Size(640, 480);
            imageProcessor = new ImageProcessor(imageSize, dispatcher);

            ThreadStart start = new ThreadStart(DoGeneratePointsWorker);
            generationThread = new Thread(start);

            IsGeneratorThreadRunning = true;
            generationThread.Start();
        }

        public void StopGenerating()
        {
            IsGeneratorThreadRunning = false;
            if (generationThread == null)
                return;
            if (generationThread.IsAlive)
            {
                generationThread.Join(TimeSpan.FromMilliseconds(100));
            }

            if (generationThread.IsAlive)
            {
                generationThread.Abort();
            }
        }

        #endregion

        #region Private Methods

        private void DoGeneratePointsWorker()
        {
            VerifyInit();

            while (IsGeneratorThreadRunning)
            {
                try
                {
                    if (Application.Current == null)
                        break;
                    context.WaitAndUpdateAll();
                }
                catch (XnStatusException ex)
                {
                    Trace.WriteLine("XnStatusException: " + ex.ToString());
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Exception: " + ex.ToString());
                    IsGeneratorThreadRunning = false;
                }
            }
        }

        private void VerifyInit()
        {
            if (context != null)
            {
                return;
            }
            string initFile = @"data/openni.xml";

            bool isInit = false;
            while (!isInit)
            {
                try
                {
                    context = new Context(initFile);
                    isInit = true;
                }
                catch (XnStatusException ex)
                {
                    Trace.WriteLine("XnStatusException: " + ex.ToString());
                    isInit = false;
                    Thread.Sleep(1000);
                }
                catch (GeneralException ex)
                {
                    Trace.WriteLine("GeneralException: " + ex.ToString());
                    isInit = false;
                    IsGeneratorThreadRunning = false;
                    return;
                }
            }

            this.joints = new Dictionary<uint, Dictionary<SkeletonJoint, SkeletonJointPosition>>();

            this.userGenerator = new UserGenerator(this.context);
            this.skeletonCapability = new SkeletonCapability(this.userGenerator);
            this.poseDetectionCapability = new PoseDetectionCapability(this.userGenerator);
            this.calibPose = this.skeletonCapability.GetCalibrationPose();

            this.userGenerator.NewUser += new UserGenerator.NewUserHandler(userGenerator_NewUser);
            this.userGenerator.LostUser += new UserGenerator.LostUserHandler(userGenerator_LostUser);
            this.poseDetectionCapability.PoseDetected += new PoseDetectionCapability.PoseDetectedHandler(poseDetectionCapability_PoseDetected);
            this.skeletonCapability.CalibrationEnd += new SkeletonCapability.CalibrationEndHandler(skeletonCapability_CalibrationEnd);

            this.skeletonCapability.SetSkeletonProfile(SkeletonProfile.Upper);
            this.userGenerator.StartGenerating();

            depthGenerator = context.FindExistingNode(NodeType.Depth) as DepthGenerator;

            depthGenerator.NewDataAvailable += new StateChangedHandler(depthGenerator_NewDataAvailable);

            this.histogram = new int[depthGenerator.GetDeviceMaxDepth()];

            depthGenerator.StartGenerating();

        }

        #region Skeleton Methods

        void skeletonCapability_CalibrationEnd(ProductionNode node, uint id, bool success)
        {
            if (success)
            {
                this.skeletonCapability.StartTracking(id);
                this.joints.Add(id, new Dictionary<SkeletonJoint, SkeletonJointPosition>());
                this.skeletonCapability.SaveCalibrationData(id, 0);
                isFirstCalibrationComplete = true;
                OnSkeletonReady((int)id);
            }
            else
            {
               // this.skeletonCapability.RequestCalibration(id, true);
                this.poseDetectionCapability.StartPoseDetection(calibPose, id);
                OnUserFound((int)id);
                isFirstPoseAttempted = false;
            }
        }

        void poseDetectionCapability_PoseDetected(ProductionNode node, string pose, uint id)
        {
            if (isFirstPoseAttempted || this.skeletonCapability.IsCalibrating(id))
                return;
            isFirstPoseAttempted = true;

            this.poseDetectionCapability.StopPoseDetection(id);
            this.skeletonCapability.RequestCalibration(id, true);
            OnPoseRecognized((int)id);
        }

        void userGenerator_NewUser(ProductionNode node, uint id)
        {
            if (isFirstCalibrationComplete)
            {
                OnUserFound((int)id);
                this.skeletonCapability.LoadCalibrationData(id, 0);
                this.skeletonCapability.StartTracking(id);
                this.joints.Add(id, new Dictionary<SkeletonJoint, SkeletonJointPosition>());
                OnSkeletonReady((int)id);
            }
            else if (!isFirstPoseAttempted)
            {
                this.poseDetectionCapability.StartPoseDetection(this.calibPose, id);
                OnUserFound((int)id);
            }
        }

        void userGenerator_LostUser(ProductionNode node, uint id)
        {
            if (joints.ContainsKey(id))
            {
                this.joints.Remove(id);
                DestroyHandSession((int)id * 2);
                DestroyHandSession((int)id * 2 + 1);
            }
            OnUserLost((int)id);
        }

        private void GetJoint(uint user, SkeletonJoint joint)
        {
            SkeletonJointPosition pos = new SkeletonJointPosition();
            this.skeletonCapability.GetSkeletonJointPosition(user, joint, ref pos);
            if (pos.position.Z == 0)
            {
                pos.fConfidence = 0;
            }
            else
            {
                //pos.position = this.depthGenerator.ConvertRealWorldToProjective(pos.position);
            }
            lock (joints)
            {
                try
                {
                    if (!this.joints.ContainsKey(user))
                    {
                        this.joints.Add(user, new Dictionary<SkeletonJoint, SkeletonJointPosition>());
                    }
                    else if (!this.joints[user].ContainsKey(joint))
                    {
                        this.joints[user].Add(joint, pos);
                    }
                    else
                    {
                        this.joints[user][joint] = pos;
                    }
                }
                catch (NullReferenceException ex)
                {
                    //eat it
                }
            }
        }

        private void GetJoints(uint user)
        {
            GetJoint(user, SkeletonJoint.Head);
            GetJoint(user, SkeletonJoint.Neck);

            GetJoint(user, SkeletonJoint.LeftShoulder);
            GetJoint(user, SkeletonJoint.LeftElbow);
            GetJoint(user, SkeletonJoint.LeftHand);

            GetJoint(user, SkeletonJoint.RightShoulder);
            GetJoint(user, SkeletonJoint.RightElbow);
            GetJoint(user, SkeletonJoint.RightHand);

            GetJoint(user, SkeletonJoint.Torso);

            //GetJoint(user, SkeletonJoint.LeftHip);
            //GetJoint(user, SkeletonJoint.LeftKnee);
            //GetJoint(user, SkeletonJoint.LeftFoot);

            //GetJoint(user, SkeletonJoint.RightHip);
            //GetJoint(user, SkeletonJoint.RightKnee);
            //GetJoint(user, SkeletonJoint.RightFoot);
        }

        private void DrawLine(byte[] fullmap, Color color, Dictionary<SkeletonJoint, SkeletonJointPosition> dict, SkeletonJoint j1, SkeletonJoint j2)
        {
            xn.Point3D pos1 = this.depthGenerator.ConvertRealWorldToProjective(dict[j1].position);
            xn.Point3D pos2 = this.depthGenerator.ConvertRealWorldToProjective(dict[j2].position);

            if (dict[j1].fConfidence == 0 || dict[j2].fConfidence == 0)
                return;

            float deltaX = pos2.X - pos1.X;
            float deltaY = pos2.Y - pos1.Y;
            float maxDelta = Math.Max(deltaX, deltaY);
            float curX = pos1.X;
            float curY = pos1.Y;

            for (int i = 0; i < maxDelta; i++)
            {
                int index = (int)(curX * 3 + curY * 640 * 3);

                fullmap[index] = color.R;
                fullmap[index + 1] = color.G;
                fullmap[index + 2] = color.B;

                curX += deltaX / maxDelta;
                curY += deltaY / maxDelta;
            }
        }

        private void DrawSkeleton(byte[] map, Color color, uint user)
        {
            GetJoints(user);
            Dictionary<SkeletonJoint, SkeletonJointPosition> dict = this.joints[user];

            DrawLine(map, color, dict, SkeletonJoint.Head, SkeletonJoint.Neck);

            DrawLine(map, color, dict, SkeletonJoint.LeftShoulder, SkeletonJoint.Torso);
            DrawLine(map, color, dict, SkeletonJoint.RightShoulder, SkeletonJoint.Torso);

            DrawLine(map, color, dict, SkeletonJoint.Neck, SkeletonJoint.LeftShoulder);
            DrawLine(map, color, dict, SkeletonJoint.LeftShoulder, SkeletonJoint.LeftElbow);
            DrawLine(map, color, dict, SkeletonJoint.LeftElbow, SkeletonJoint.LeftHand);

            DrawLine(map, color, dict, SkeletonJoint.Neck, SkeletonJoint.RightShoulder);
            DrawLine(map, color, dict, SkeletonJoint.RightShoulder, SkeletonJoint.RightElbow);
            DrawLine(map, color, dict, SkeletonJoint.RightElbow, SkeletonJoint.RightHand);

            DrawLine(map, color, dict, SkeletonJoint.LeftHip, SkeletonJoint.Torso);
            DrawLine(map, color, dict, SkeletonJoint.RightHip, SkeletonJoint.Torso);
            DrawLine(map, color, dict, SkeletonJoint.LeftHip, SkeletonJoint.RightHip);

            DrawLine(map, color, dict, SkeletonJoint.LeftHip, SkeletonJoint.LeftKnee);
            DrawLine(map, color, dict, SkeletonJoint.LeftKnee, SkeletonJoint.LeftFoot);

            DrawLine(map, color, dict, SkeletonJoint.RightHip, SkeletonJoint.RightKnee);
            DrawLine(map, color, dict, SkeletonJoint.RightKnee, SkeletonJoint.RightFoot);
        }

        #endregion

        unsafe void depthGenerator_NewDataAvailable(ProductionNode node)
        {
            if (Application.Current == null)
            {
                IsGeneratorThreadRunning = false;
                return;
            }
            DepthMetaData depthMD = new DepthMetaData();

            depthGenerator.GetMetaData(depthMD);

            if (depthMD.DataSize == 0)
                return;

            int numPixels = (int)(depthMD.XRes * depthMD.YRes);

            ushort* sourceData = (ushort*)depthMD.DepthMapPtr.ToPointer();

            Image sourceImage = imageProcessor.Factory.CreateImage(depthMD.FullXRes, depthMD.FullYRes);
            CopyImageData(numPixels, sourceData, sourceImage);

            UpdateSessions();

            imageProcessor.ProcessDepthSessions(sourceImage, HandSessions);

            if (!firstFrameNotified)
            {
                OnFirstFrameReady();
                firstFrameNotified = true;
            }

            OnFrameUpdated();

        }

        void Comments()
        {
            /*
            //copy so we don't have to lock
            var sessions = new Dictionary<int, HandSession>(HandSessions);

            if (sessions.Count != 0)
            {
                //int rightOffset = 1;
                //int leftOffset = -1;
                //int downOffset = srcStride;
                //int upOffset = -srcStride;
                //int rightDownOffset = rightOffset + downOffset;
                //int rightUpOffset = rightOffset + upOffset;
                //int leftDownOffset = leftOffset + downOffset;
                //int leftUpOffset = leftOffset + upOffset;

                //For each tracked hand
                foreach (var kvp in sessions)
                {
                    HandSession hand = kvp.Value;
                    hand.PolarPoints.Clear();
                    
                    //Get the image-centric hand position
                    xn.Point3D projective = depthGenerator.ConvertRealWorldToProjective(hand.xnPosition);
                    
                    int x = (int)projective.X;
                    int y = (int)projective.Y;

                    ushort[] handMapDepthVals = new ushort[hand.XRes * hand.YRes];
                    byte[] handMap = hand.HandMap;
                    byte[] handMapDepth = hand.HandMapDepth;

                    if (x < 0 || x >= XRes ||
                        y < 0 || y >= YRes)
                        continue;
                    ushort targetDepth = sourceData[x + y * srcStride];

                    Image handImage = imageProcessor.CropHandImage(sourceImage, x, y, 
                                                                  (ushort)(targetDepth - 100),
                                                                  (ushort)(targetDepth + 100));

                    imageProcessor.SaveImageToBytes(handImage, ref handMap);
             */
            /*
            int avgX = 0;
            int avgY = 0;
            int avgCount = 0;
            //For a 200x200 pixel box centered on the hand position
            int handStride = hand.Stride;
            int handXRes = hand.XRes;
            for (int mapX = 0; mapX < 200; mapX++)
            {
                for (int mapY = 0; mapY < 200; mapY++)
                {
                    int mapIndex = mapX * 4 + mapY * handStride;
                    int mapIndexVals = mapX + mapY * handXRes;

                    for (int k = 0; k < 4; k++)
                    {
                        handMap[mapIndex + k] = 0;
                        handMapDepth[mapIndex + k] = 0;
                    }

                    int curX = mapX + x - 100;
                    int curY = mapY + y - 100;

                    if (curX <= 1 || curX >= XRes - 1 ||
                        curY <= 1 || curY >= YRes - 1)
                    {
                        continue;
                    }

                    int i = curX + curY * srcStride;

                    ushort value = sourceData[i];

                    //Threshold the depth 
                    if (value > 400 &&
                        value > targetDepth - 100 &&
                        value < targetDepth + 100)
                    {
                        //Keep the depth visual
                        //filteredData[i] = value;
                        //fastBitmap.SetPixel(curX, curY, value);
                        //gradientData[i] = 0;
                        //fastGradient.SetPixel(curX, curY, 0);
                        //fastGradient.Bitmap[i] = 0;
                        for (int m = 0; m < 3; m++)
                        {
                            handMapDepthVals[mapIndexVals] = value;
                        }
                        handMapDepth[mapIndex + 3] = 255;
                        handMap[mapIndex + 0] = 255;
                        handMap[mapIndex + 3] = 255;
                        /*
                        //Do edge detection
                        int threshold = 40;
                        if ((value - sourceData[i + rightOffset]) > threshold ||
                            (value - sourceData[i + leftOffset]) > threshold ||
                            (value - sourceData[i + upOffset]) > threshold ||
                            (value - sourceData[i + downOffset]) > threshold ||
                            (value - sourceData[i + rightDownOffset]) > threshold ||
                            (value - sourceData[i + rightUpOffset]) > threshold ||
                            (value - sourceData[i + leftDownOffset]) > threshold ||
                            (value - sourceData[i + leftUpOffset]) > threshold ||

                            (value - sourceData[i + rightOffset]) < -threshold ||
                            (value - sourceData[i + leftOffset]) < -threshold ||
                            (value - sourceData[i + upOffset]) < -threshold ||
                            (value - sourceData[i + downOffset]) < -threshold ||
                            (value - sourceData[i + rightDownOffset]) < -threshold ||
                            (value - sourceData[i + rightUpOffset]) < -threshold ||
                            (value - sourceData[i + leftDownOffset]) < -threshold ||
                            (value - sourceData[i + leftUpOffset]) < -threshold)
                        {
                            //gradientData[i] = 1;
                            //fastGradient.SetPixel(curX, curY, 1);
                            //fastGradient.Bitmap[i] = 1;
                            hand.PolarPoints.AddPoint(new Point2D(curX, curY));

                            handMap[mapIndex + 0] = 255;
                            handMap[mapIndex + 3] = 255;

                            avgX += curX;
                            avgY += curY;
                            avgCount++;
                        }
                         */
            /*
                        avgX += curX;
                        avgY += curY;
                        avgCount++;
                    }
                }
            }
            if (avgCount == 0)
                continue;
            avgX /= avgCount;
            avgY /= avgCount;

            hand.PolarPoints.UpdateCenter(new Point2D(avgX, avgY));

            CalcHist(hand.XRes, hand.YRes, handMapDepthVals);
            int j = 0;
            int interval = hand.BytesPerPixel;
            int limit = hand.HandMapSize;
            for (int i = 0; i < limit; i += interval, j++)
            {
                handMapDepth[i] = (byte)histogram[handMapDepthVals[j]];
                handMapDepth[i + 1] = (byte)histogram[handMapDepthVals[j]];
                handMapDepth[i + 2] = (byte)histogram[handMapDepthVals[j]];
            }
            */
            /*
                }
            }
            

            if (DepthUpdated != null)
            {
                //ushort* pLabels = (ushort*)this.userGenerator.GetUserPixels(0).SceneMapPtr.ToPointer();
                CalcHist(XRes, YRes, filteredData);

                int j = 0;

                byte[] fullmap = new byte[size];
                for (int i = 0; i < size; i += 3, j++)
                {
                    //ushort label = pLabels[j];
                    //Color labelColor = Colors.White;
                    //if (label != 0)
                    //{
                    //    labelColor = colors[label % ncolors];
                    //}

                    //fullmap[i] = (byte)(histogram[filteredData[j]] * (labelColor.R / 256.0));
                    //fullmap[i + 1] = (byte)(histogram[filteredData[j]] * (labelColor.G / 256.0));
                    //fullmap[i + 2] = (byte)(histogram[filteredData[j]] * (labelColor.B / 256.0)); 
                    fullmap[i] = (byte)(histogram[filteredData[j]]);
                    fullmap[i + 1] = (byte)(histogram[filteredData[j]]);
                    fullmap[i + 2] = (byte)(histogram[filteredData[j]]);
                }

                //foreach (uint user in users)
                //{
                //    if (this.skeletonCapability.IsTracking(user))
                //        DrawSkeleton(fullmap, anticolors[user % ncolors], user);
                //}
                OnDepthUpdated(fullmap, XRes, YRes, rgbStride, PixelFormats.Rgb24);
            }
            //PolarCoord co = new PolarCoord(new Point2D(20, 20));
            //co.CalculateCenter(new Point2D(0, 0), new Vector(0, 1));
            //co.UpdatePosition(new Point2D(0, 0));

            if (DataMapUpdated == null)
                return;


            foreach (var kvp in sessions)
            {
                var hand = kvp.Value;

                foreach (var coord in hand.PolarPoints.Points)
                {
                    int index = BitmapHelper.CoordinateToIndex(coord.Position.X, coord.Position.Y, 3, rgbStride);
                    map[index] = 0;
                    map[index + 1] = 0;
                    map[index + 2] = 255;
                }

                hand.Circles.Clear();
                hand.Bins.Clear();

                //Find the wrist gap
                //int currentGapCount = 0;
                //int currentGapStart = -1;
                //int maxGapCount = 0;
                //int angleOffset = 0;
                //int gapThreshold = 10;
                //bool previousWasGap = false;
                //for (int i = 0; i < 360; i++)
                //{
                //    var coords = hand.PolarPoints.GetCoordsByAngle(i);
                //    if (coords.Count() == 0)
                //    {
                //        currentGapCount++;
                //        if (!previousWasGap)
                //        {
                //            currentGapStart = i;
                //        }
                //        if (currentGapCount > maxGapCount)
                //        {
                //            maxGapCount = currentGapCount;
                //            angleOffset = currentGapStart;
                //        }
                //    }
                //    else
                //    {
                //        currentGapCount = 0;

                //        previousWasGap = false;
                //    }
                //}

                //if (angleOffset < gapThreshold)
                //{
                //    angleOffset = 0;
                //}
                //else
                //{
                //    for (int i = angleOffset - 40 - maxGapCount; i < angleOffset + 40; i++)
                //    {
                //        int index = (int)MathUtility.NormalizeAngle(i);
                //        var coords = hand.PolarPoints.GetCoordsByAngle(index);
                //        if (coords.Count() == 0)
                //        {
                //            continue;
                //        }
                //        else
                //        {
                //            coords.ToList().ForEach(c =>
                //                {
                //                    c.Radius = 30;
                //                    //c.UpdatePosition(hand.PolarPoints.Center);
                //                });
                //        }
                //    }
                //}

                {
                    //smooth the lines
                    //double minRadius = double.MaxValue;
                    //double avgRadius = 0;
                    //double avgCount = 0;
                    //double squareSum = 0;

                    for (int i = 0; i < 360; i++)
                    {
                        var coords = hand.PolarPoints.GetCoordsByAngle(i);
                        if (coords.Count() == 0)
                        {
                            continue;
                        }

                        double value = 0;
                        int count = 0;

                        for (int k = -15; k <= 15; k++)
                        {
                            int index = (int)MathUtility.NormalizeAngle(i + k);
                            var coordsLoop = hand.PolarPoints.GetCoordsByAngle(index);
                            if (coordsLoop.Count() != 0)
                            {
                                value += coordsLoop.ToList().Max(c => c.Radius);
                                count++;
                            }
                        }


                        if (count == 0)
                        {
                            count = 1;
                            value = 0;
                        }
                        value /= count;

                        coords.ToList().ForEach(c =>
                        {
                            c.Radius = value;
                        });

                        //squareSum += value * value;
                        //if (value < minRadius)
                        //    minRadius = value;
                        //avgRadius += value;
                        //avgCount++;
                    }
                    hand.PolarPoints.UpdateCenter(hand.PolarPoints.Center);

                    //squareSum /= avgCount;
                    //avgRadius /= avgCount;
                    //double stddev = Math.Sqrt(squareSum - avgRadius * avgRadius);

                    //hand.Circles.Add(new PolarCoord()
                    //{
                    //    Radius = minRadius + stddev,
                    //    Position = hand.PolarPoints.Center
                    //});
                    hand.Circles.Add(new PolarCoord()
                    {
                        Radius = hand.PolarPoints.MinRadius,
                        Position = hand.PolarPoints.Center
                    });

                    //PolarCoordCollection bin = new PolarCoordCollection();
                    //bin.UpdateCenter(hand.PolarPoints.Center);
                    //hand.Bins.Add(bin);
                    //for (int i = 0; i < 360; i++)
                    //{
                    //    var coords = hand.PolarPoints.GetCoordsByAngle(i);
                    //    if (coords.Count() == 0)
                    //        continue;
                    //    foreach (var coord in coords)
                    //    {
                    //        if (coord.Radius < minRadius + stddev)
                    //        {
                    //            bin.AddPoint(coord);
                    //            coord.Radius = minRadius;
                    //            coord.IsClassified = true;
                    //        }
                    //    }
                    //}
                }
                //Segment sub-bins
                //List<PolarCoordCollection> bins = new List<PolarCoordCollection>();
                //PolarCoordCollection currentBin = new PolarCoordCollection();
                //bins.Add(currentBin);
                //bool wasLastEmpty = false;
                //for (int i = 0; i < 360; i++)
                //{
                //    var coords = hand.PolarPoints.GetCoordsByAngle(i);
                //    if (coords.Count() == 0)
                //    {
                //        continue;
                //    }
                //    if (coords.FirstOrDefault(c => c.IsClassified) != null)
                //    {
                //        if (!wasLastEmpty)
                //        {
                //            currentBin = new PolarCoordCollection();
                //            bins.Add(currentBin);
                //        }
                //        wasLastEmpty = true;
                //        continue;
                //    }
                //    wasLastEmpty = false;
                //    coords.ToList().ForEach(c => currentBin.AddPoint(c.Position));
                //    //coords.ToList().ForEach(currentBin.AddPoint);
                //}

                //foreach (var collection in bins)
                //{
                //    hand.Bins.Add(collection);
                //    int avgX = 0;
                //    int avgY = 0;
                //    int avgCount = 0;
                //    collection.Points.ToList().ForEach(c =>
                //        {
                //            avgX += c.Position.X;
                //            avgY += c.Position.Y;
                //            avgCount++;
                //        });
                //    if (avgCount == 0)
                //        continue;

                //    avgX /= avgCount;
                //    avgY /= avgCount;

                //    collection.UpdateCenter(new Point2D(avgX, avgY));

                //    double minRadius = collection.Points.ToList().Min(c => c.Radius);
                //    hand.Circles.Add(new PolarCoord()
                //    {
                //        Radius = minRadius,
                //        Position = collection.Center
                //    });

                //    foreach (var coord in collection.Points)
                //    {
                //        if (coord.Radius < minRadius + 20)
                //        {
                //            coord.Radius = minRadius;
                //            coord.IsClassified = true;
                //        }
                //    }
                //}
            */

            //Display everything
            /*
            Point position = new Point(10, 470);
            xn.Point3D projective = depthGenerator.ConvertRealWorldToProjective(hand.Position);
            Point reCenter = new Point(projective.X + 150, projective.Y);
            position = new Point(projective.X - 90, projective.Y + 200);
            for (int i = 0; i < 360; i += 1)
            {
                int normAngle = (int)MathUtility.NormalizeAngle(i + angleOffset - maxGapCount + 1);
                var coords = hand.PolarPoints.GetCoordsByAngle(normAngle);

                if (coords.Count() == 0)
                {
                    continue;
                }

                int yStart = (int)position.Y;
                double radius = coords.ToList().Max(c => c.Radius);

                for (int y = yStart; y > yStart - radius; y--)
                {
                    if (position.X > 0 &&
                        position.X < depthMD.XRes &&
                        y > 0 &&
                        y < depthMD.YRes)
                    {
                        int index = (int)position.X * 3 + y * rgbStride;
                        map[index] = 0;
                        map[index + 1] = 255;
                        map[index + 2] = 0;
                    }
                }
                position.X += 1;
            }

            foreach (var bin in hand.Bins)
            {
                foreach (var coord in bin.Points)
                {
                    int x2 = (int)(bin.Center.X + 150 + Math.Cos((coord.Angle + 90) * Math.PI / 180.0) * coord.Radius);
                    int y2 = (int)(bin.Center.Y + Math.Sin((coord.Angle + 90) * Math.PI / 180.0) * coord.Radius);
                    if (x2 > 0 &&
                        x2 < depthMD.XRes &&
                        y2 > 0 &&
                        y2 < depthMD.YRes)
                    {
                        int index2 = (int)x2 * 3 + y2 * rgbStride;

                        map[index2] = 255;
                        map[index2 + 1] = 255;
                        map[index2 + 2] = 255;
                    }
                }

            }
            */
            /*
                foreach (var circle in hand.Circles)
                {
                    for (int m = 0; m < 360; m++)
                    {
                        int x2 = (int)(circle.Position.X + Math.Cos(m * Math.PI / 180.0) * circle.Radius);
                        int y2 = (int)(circle.Position.Y + Math.Sin(m * Math.PI / 180.0) * circle.Radius);
                        if (x2 > 0 &&
                            x2 < depthMD.XRes &&
                            y2 > 0 &&
                            y2 < depthMD.YRes)
                        {
                            int index2 = (int)x2 * 3 + y2 * rgbStride;

                            map[index2] = 255;
                            map[index2 + 1] = 255;
                            map[index2 + 2] = 255;
                        }
                    }
                }
            }
            OnDataMapUpdated(map, XRes, YRes, rgbStride, PixelFormats.Rgb24);
            */
        }

        unsafe private static void CopyImageData(int numPixels, ushort* sourceData, Image sourceImage)
        {

            var imageData = sourceImage.Lock(DirectCanvas.Imaging.ImageLock.ReadWrite);

            //Image is BGRA format, 4 bytes per pixel
            byte* imagePtr = (byte*)imageData.Scan0.ToPointer();

            for (int i = 0; i < numPixels; i++, sourceData++, imagePtr += 4)
            {
                //ushort value = sourceData[i];

                //pack ushort into first two bytes of pixel
                //((ushort*)imagePtr)[i * 2] = value;
                *(ushort*)imagePtr = *sourceData;
                //imagePtr[i * 4] = (byte)(value & 255); //low byte in B
                //imagePtr[i * 4 + 1] = (byte)(value >> 8); //high byte in G

                //store 255 in Alpha channel of pixel
                //imagePtr[i * 4 + 3] = 255;
                *(imagePtr + 3) = 255;
            }

            sourceImage.Unlock(imageData);
        }

        unsafe private void UpdateSessions()
        {
            uint[] users = this.userGenerator.GetUsers();
            foreach (uint user in users)
            {
                if (this.skeletonCapability.IsTracking(user))
                {
                    GetJoints(user);
                    if (!this.joints.ContainsKey(user))
                        continue;
                    Dictionary<SkeletonJoint, SkeletonJointPosition> dict = this.joints[user];


                    if (dict[SkeletonJoint.LeftShoulder].fConfidence > 0.5 &&
                        dict[SkeletonJoint.LeftHand].fConfidence > 0.5)
                    {
                        xn.Point3D leftShoulder = dict[SkeletonJoint.LeftShoulder].position;
                        xn.Point3D leftHand = dict[SkeletonJoint.LeftHand].position;

                        UpdateHandSession((int)user * 2, leftHand, leftShoulder);
                    }
                    else
                    {
                        DestroyHandSession((int)user * 2);
                    }

                    if (dict[SkeletonJoint.RightShoulder].fConfidence > 0.5 &&
                        dict[SkeletonJoint.RightHand].fConfidence > 0.5)
                    {
                        xn.Point3D rightShoulder = dict[SkeletonJoint.RightShoulder].position;
                        xn.Point3D rightHand = dict[SkeletonJoint.RightHand].position;
                        UpdateHandSession((int)user * 2 + 1, rightHand, rightShoulder);
                    }
                    else
                    {
                        DestroyHandSession((int)user * 2 + 1);
                    }
                }
            }
        }

        unsafe private static void ProcessRect(DepthMetaData depthMD, int srcStride, ushort[] filteredData, ushort[] gradientData, HandSession hand, int x, int y)
        {
            int sizeRight = 99;
            int sizeLeft = 99;
            int sizeUp = 99;
            int sizeDown = 99;
            //Find the largest rectangle that fits
            for (int i = 0; i < 100; i++)
            {
                if (sizeRight == 99 &&
                    x + i > 2 &&
                    x + i < depthMD.XRes - 2)
                {
                    if (filteredData[(x + i) + y * srcStride] > 400)
                    {
                        gradientData[(x + i) + y * srcStride] = 2;
                    }
                    else
                    {
                        sizeRight = i;
                    }
                }
                if (sizeLeft == 99 &&
                    x - i > 2 &&
                    x - i < depthMD.XRes - 2)
                {
                    if (filteredData[(x - i) + y * srcStride] > 400)
                    {
                        gradientData[(x - i) + y * srcStride] = 2;
                    }
                    else
                    {
                        sizeLeft = i;
                    }
                }

                if (sizeDown == 99 &&
                    y + i > 2 &&
                    y + i < depthMD.YRes - 2)
                {
                    if (filteredData[x + (y + i) * srcStride] > 400)
                    {
                        gradientData[x + (y + i) * srcStride] = 2;
                    }
                    else
                    {
                        sizeDown = i;
                    }
                }
                if (sizeUp == 99 &&
                    y - i > 2 &&
                    y - i < depthMD.YRes - 2)
                {
                    if (filteredData[x + (y - i) * srcStride] > 400)
                    {
                        gradientData[x + (y - i) * srcStride] = 2;
                    }
                    else
                    {
                        sizeUp = i;
                    }
                }
            }

            int rightDown = 99;
            int rightUp = 99;
            int leftDown = 99;
            int leftUp = 99;

            int sizeRightA = (int)(sizeRight * 0.75);
            int sizeLeftA = (int)(sizeLeft * 0.75);

            for (int i = 0; i < 100; i++)
            {
                if (rightDown == 99 &&
                    y + i > 2 &&
                    y + i < depthMD.YRes - 2)
                {
                    if (filteredData[x + sizeRightA + (y + i) * srcStride] > 400)
                    {
                        gradientData[x + sizeRightA + (y + i) * srcStride] = 3;
                    }
                    else
                    {
                        rightDown = i;
                    }
                }

                if (rightUp == 99 &&
                    y - i > 2 &&
                    y - i < depthMD.YRes - 2)
                {
                    if (filteredData[x + sizeRightA + (y - i) * srcStride] > 400)
                    {
                        gradientData[x + sizeRightA + (y - i) * srcStride] = 3;
                    }
                    else
                    {
                        rightUp = i;
                    }
                }

                if (leftDown == 99 &&
                    y + i > 2 &&
                    y + i < depthMD.YRes - 2)
                {
                    if (filteredData[x - sizeLeftA + (y + i) * srcStride] > 400)
                    {
                        gradientData[x - sizeLeftA + (y + i) * srcStride] = 3;
                    }
                    else
                    {
                        leftDown = i;
                    }
                }

                if (leftUp == 99 &&
                    y - i > 2 &&
                    y - i < depthMD.YRes - 2)
                {
                    if (filteredData[x - sizeLeftA + (y - i) * srcStride] > 400)
                    {
                        gradientData[x - sizeLeftA + (y - i) * srcStride] = 3;
                    }
                    else
                    {
                        leftUp = i;
                    }
                }
            }
            int sizeUpA = Math.Min(leftUp, rightUp);
            int sizeDownA = Math.Min(leftDown, rightDown);

            int rightDown2 = 99;
            int rightUp2 = 99;
            int leftDown2 = 99;
            int leftUp2 = 99;

            int sizeDownB = (int)(sizeDown * 0.75);
            int sizeUpB = (int)(sizeUp * 0.75);

            for (int i = 0; i < 100; i++)
            {
                if (rightDown2 == 99 &&
                    x + i > 2 &&
                    x + i < depthMD.XRes - 2)
                {
                    if (filteredData[x + i + (y + sizeDownB) * srcStride] > 400)
                    {
                        gradientData[x + i + (y + sizeDownB) * srcStride] = 3;
                    }
                    else
                    {
                        rightDown2 = i;
                    }
                }

                if (rightUp2 == 99 &&
                    x + i > 2 &&
                    x + i < depthMD.XRes - 2)
                {
                    if (filteredData[x + i + (y - sizeUpB) * srcStride] > 400)
                    {
                        gradientData[x + i + (y - sizeUpB) * srcStride] = 3;
                    }
                    else
                    {
                        rightUp2 = i;
                    }
                }

                if (leftDown2 == 99 &&
                    x - i > 2 &&
                    x - i < depthMD.XRes - 2)
                {
                    if (filteredData[x - i + (y + sizeDownB) * srcStride] > 400)
                    {
                        gradientData[x - i + (y + sizeDownB) * srcStride] = 3;
                    }
                    else
                    {
                        leftDown2 = i;
                    }
                }

                if (leftUp2 == 99 &&
                    x - i > 2 &&
                    x - i < depthMD.XRes - 2)
                {
                    if (filteredData[x - i + (y - sizeUpB) * srcStride] > 400)
                    {
                        gradientData[x - i + (y - sizeUpB) * srcStride] = 3;
                    }
                    else
                    {
                        leftUp2 = i;
                    }
                }
            }
            int sizeRightB = Math.Min(rightUp, rightDown);
            int sizeLeftB = Math.Min(leftUp, leftDown);

            if ((sizeRightA + sizeLeftA) * (sizeUpA + sizeUpA) >
                (sizeRightB + sizeLeftB) * (sizeUpB + sizeUpB))
            {
                sizeRight = sizeRightA;
                sizeLeft = sizeLeftA;
                sizeUp = sizeUpA;
                sizeDown = sizeDownA;
            }
            else
            {
                sizeRight = sizeRightB;
                sizeLeft = sizeLeftB;
                sizeUp = sizeUpB;
                sizeDown = sizeDownB;
            }

            Size handSize = new Size(sizeRight + sizeLeft, sizeUp + sizeDown);
            hand.Rect = new Rect(new Point(x - sizeLeft, y - sizeUp), handSize);
        }

        #region Hand Sessions

        private void UpdateHandSession(int id, xn.Point3D position, xn.Point3D shoulderPosition)
        {
            lock (HandSessions)
            {
                if (!HandSessions.ContainsKey(id))
                {
                    var session = new HandSession();
                    //session.PoseChanged += session_PoseChanged;
                    session.Id = id;
                    HandSessions.Add(session.Id, session);
                }

                HandSessions[id].xnPosition = position;
                HandSessions[id].Position = MotionHelper.XnPoint3DToPoint3D(position);
                HandSessions[id].ShoulderPosition = MotionHelper.XnPoint3DToPoint3D(shoulderPosition);
                xn.Point3D projective = depthGenerator.ConvertRealWorldToProjective(position);
                HandSessions[id].PositionProjective = MotionHelper.XnPoint3DToPoint3D(projective);
                OnPointUpdated(id, HandSessions[id]);

            }
        }

        private void DestroyHandSession(int id)
        {
            lock (HandSessions)
            {
                if (HandSessions.ContainsKey(id))
                {
                    //HandSessions[id].PoseChanged -= session_PoseChanged;
                    var session = HandSessions[id];
                    HandSessions.Remove(id);
                    OnPointDestroyed(id, session);
                }
            }
        }

        #endregion

        private unsafe void CalcHist(int XRes, int YRes, ushort[] data)
        {
            for (int i = 0; i < this.histogram.Length; ++i)
                this.histogram[i] = 0;

            //ushort* pDepth = (ushort*)depthMD.DepthMapPtr.ToPointer();

            int j = 0;
            int points = 0;
            for (int y = 0; y < YRes; ++y)
            {
                for (int x = 0; x < XRes; ++x, ++j)
                {
                    //ushort depthVal = *pDepth;
                    ushort depthVal = data[j];
                    if (depthVal != 0)
                    {
                        this.histogram[depthVal]++;
                        points++;
                    }
                }
            }

            for (int i = 1; i < this.histogram.Length; i++)
            {
                this.histogram[i] += this.histogram[i - 1];
            }

            if (points > 0)
            {
                for (int i = 1; i < this.histogram.Length; i++)
                {
                    this.histogram[i] = (int)(256 * (1.0f - (this.histogram[i] / (float)points)));
                }
            }
        }

        #endregion

    }
}
