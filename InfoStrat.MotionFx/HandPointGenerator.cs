using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using OpenNI;
using System.IO;
using System.Runtime.InteropServices;

namespace InfoStrat.MotionFx
{
    public class HandPointGenerator
    {
        bool isFirstPoseAttempted = false;
        string skeletonFile = "skeleton.data";
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

        bool firstFrameNotified = false;

        Thread generationThread;

        Context context;
        DepthGenerator depthGenerator;

        private UserGenerator userGenerator;
        private SkeletonCapability skeletonCapability;
        private PoseDetectionCapability poseDetectionCapability;
        private string calibPose;
        private Dictionary<int, Dictionary<SkeletonJoint, SkeletonJointPosition>> joints;

        private int[] histogram;

        Dictionary<int, HandSession> HandSessions = new Dictionary<int, HandSession>();

        private Color[] colors = { Colors.Red, Colors.Blue, Colors.ForestGreen, Colors.Yellow, Colors.Orange, Colors.Purple, Colors.White };
        private Color[] anticolors = { Colors.Green, Colors.Orange, Colors.Red, Colors.Purple, Colors.Blue, Colors.Yellow, Colors.Black };
        
        ushort[] pixels;
        #endregion

        #region Properties


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

        public event EventHandler<FrameUpdatedEventArgs> FrameUpdated;

        private void OnFrameUpdated(ushort[] pixels, int width, int height)
        {
            if (FrameUpdated == null)
                return;
            var args = new FrameUpdatedEventArgs(pixels, width, height, HandSessions.Values.AsEnumerable<HandSession>());
            FrameUpdated(null, args);
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
                catch (StatusException ex)
                {
                    Trace.WriteLine("OpenNI StatusException: " + ex.ToString());
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
                catch (StatusException ex)
                {
                    Trace.WriteLine("OpenNI StatusException: " + ex.ToString());
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

            this.joints = new Dictionary<int, Dictionary<SkeletonJoint, SkeletonJointPosition>>();

            this.userGenerator = new UserGenerator(this.context);
            this.skeletonCapability = userGenerator.SkeletonCapability;
            this.poseDetectionCapability = userGenerator.PoseDetectionCapability;
            this.calibPose = this.skeletonCapability.CalibrationPose;

            this.userGenerator.NewUser += new EventHandler<NewUserEventArgs>(userGenerator_NewUser);
            this.userGenerator.LostUser += new EventHandler<UserLostEventArgs>(userGenerator_LostUser);
            this.poseDetectionCapability.PoseDetected += new EventHandler<PoseDetectedEventArgs>(poseDetectionCapability_PoseDetected);
            this.skeletonCapability.CalibrationEnd += new EventHandler<CalibrationEndEventArgs>(skeletonCapability_CalibrationEnd);

            this.skeletonCapability.SetSkeletonProfile(SkeletonProfile.Upper);
            this.userGenerator.StartGenerating();

            depthGenerator = context.FindExistingNode(NodeType.Depth) as DepthGenerator;

            depthGenerator.NewDataAvailable += new EventHandler(depthGenerator_NewDataAvailable);

            this.histogram = new int[depthGenerator.DeviceMaxDepth];

            depthGenerator.StartGenerating();

        }

        #region Skeleton Methods

        void skeletonCapability_CalibrationEnd(object sender, CalibrationEndEventArgs e)
        {
            int id = e.ID;
            if (e.Success)
            {
                this.skeletonCapability.StartTracking(id);
                this.joints.Add(id, new Dictionary<SkeletonJoint, SkeletonJointPosition>());
                //this.skeletonCapability.SaveCalibrationData(id, 0);
                this.skeletonCapability.SaveCalibrationDataToFile(id, skeletonFile);
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

        void poseDetectionCapability_PoseDetected(object sender, PoseDetectedEventArgs e)
        {
            int id = e.ID;
            if (isFirstPoseAttempted || this.skeletonCapability.IsCalibrating(id))
                return;
            isFirstPoseAttempted = true;

            this.poseDetectionCapability.StopPoseDetection(id);
            this.skeletonCapability.RequestCalibration(id, true);
            OnPoseRecognized((int)id);
        }

        void userGenerator_NewUser(object sender, NewUserEventArgs e)
        {
            int id = e.ID;
            try
            {
                if (File.Exists(skeletonFile))
                {
                    OnUserFound(id);
                    this.skeletonCapability.LoadCalibrationDataFromFile(id, "skeleton.data");
                    //this.skeletonCapability.LoadCalibrationData(id, 0);
                    this.skeletonCapability.StartTracking(id);
                    this.joints.Add(id, new Dictionary<SkeletonJoint, SkeletonJointPosition>());
                    OnSkeletonReady(id);
                }
                else
                {
                    this.poseDetectionCapability.StartPoseDetection(this.calibPose, id);
                    OnUserFound(id);
                }
            }
            catch (StatusException)
            {
                this.poseDetectionCapability.StartPoseDetection(this.calibPose, id);
                OnUserFound(id);
            }
        }


        void userGenerator_LostUser(object sender, UserLostEventArgs e)
        {
            int id = e.ID;
            if (joints.ContainsKey(id))
            {
                this.joints.Remove(id);
                DestroyHandSession(id * 2);
                DestroyHandSession(id * 2 + 1);
            }
            OnUserLost(id);
        }

        private void GetJoint(int user, SkeletonJoint joint)
        {
            SkeletonJointPosition pos = this.skeletonCapability.GetSkeletonJointPosition(user, joint);
            if (pos.Position.Z == 0)
            {
                pos.Confidence = 0;
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
                catch (NullReferenceException)
                {
                    //eat it
                }
            }
        }

        private void GetJoints(int user)
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
            OpenNI.Point3D pos1 = this.depthGenerator.ConvertRealWorldToProjective(dict[j1].Position);
            OpenNI.Point3D pos2 = this.depthGenerator.ConvertRealWorldToProjective(dict[j2].Position);

            if (dict[j1].Confidence == 0 || dict[j2].Confidence == 0)
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

        private void DrawSkeleton(byte[] map, Color color, int user)
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

        unsafe void depthGenerator_NewDataAvailable(object sender, EventArgs e)
        {
            if (Application.Current == null)
            {
                IsGeneratorThreadRunning = false;
                return;
            }
            try
            {
                DepthMetaData depthMD = new DepthMetaData();

                depthGenerator.GetMetaData(depthMD);

                if (depthMD.DataSize == 0)
                    return;

                int numPixels = (int)(depthMD.XRes * depthMD.YRes);

                //ushort* sourceData = (ushort*)depthMD.DepthMapPtr.ToPointer();
                if (pixels == null ||
                    pixels.Length != numPixels)
                {
                    pixels = new ushort[numPixels];
                }

                //Marshal.Copy(depthMD.DepthMapPtr, (byte[])pixels, 0, numPixels);
                fixed (ushort* dest = pixels)
                {
                    NativeInterop.MoveMemory((IntPtr)dest, depthMD.DepthMapPtr, numPixels * sizeof(ushort));
                }
                //for (int i = 0; i < numPixels; i++, sourceData++)
                //{
                //    pixels[i] = *sourceData;
                //}

                UpdateSessions();

                if (!firstFrameNotified)
                {
                    OnFirstFrameReady();
                    firstFrameNotified = true;
                }

                OnFrameUpdated(pixels, depthMD.XRes, depthMD.YRes);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("HandPointGenerator - ImageProcessor exception: " + ex.ToString());
            }
        }

        private void UpdateSessions()
        {
            int[] users = this.userGenerator.GetUsers();
            foreach (int user in users)
            {
                if (this.skeletonCapability.IsTracking(user))
                {
                    GetJoints(user);
                    if (!this.joints.ContainsKey(user))
                        continue;
                    Dictionary<SkeletonJoint, SkeletonJointPosition> dict = this.joints[user];

                    if (dict[SkeletonJoint.LeftShoulder].Confidence > 0.5 &&
                        dict[SkeletonJoint.LeftHand].Confidence > 0.5)
                    {
                        OpenNI.Point3D leftShoulder = dict[SkeletonJoint.LeftShoulder].Position;
                        OpenNI.Point3D leftHand = dict[SkeletonJoint.LeftHand].Position;

                        UpdateHandSession((int)user * 2, leftHand, leftShoulder);
                    }
                    else
                    {
                        DestroyHandSession((int)user * 2);
                    }

                    if (dict[SkeletonJoint.RightShoulder].Confidence > 0.5 &&
                        dict[SkeletonJoint.RightHand].Confidence > 0.5)
                    {
                        OpenNI.Point3D rightShoulder = dict[SkeletonJoint.RightShoulder].Position;
                        OpenNI.Point3D rightHand = dict[SkeletonJoint.RightHand].Position;
                        UpdateHandSession((int)user * 2 + 1, rightHand, rightShoulder);
                    }
                    else
                    {
                        DestroyHandSession((int)user * 2 + 1);
                    }
                }
            }
        }

        #region Hand HandSessions

        private void UpdateHandSession(int id, OpenNI.Point3D position, OpenNI.Point3D shoulderPosition)
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
                OpenNI.Point3D projective = depthGenerator.ConvertRealWorldToProjective(position);
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
