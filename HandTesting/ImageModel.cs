using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HandTesting
{
    public class ImageModel
    {
        public Uri ImageSource { get; set; }

        public ImageModel(Uri source)
        {
            this.ImageSource = source;
        }
    }
}
