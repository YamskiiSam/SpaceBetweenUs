﻿using OpenCvSharp;
using OpenCvSharp.Dnn;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alturos.Yolo;
using Alturos.Yolo.Model;
using Microsoft.Win32;

namespace SpaceBetweenUs.Services.Detectors
{
    public class AlturosYoloV3Detector : IDetector
    {
        #region HelpersClasses

        public class AlturosValidator : IYoloSystemValidator
        {
            public SystemValidationReport Validate()
            {
                SystemValidationReport report = new SystemValidationReport();
                report.MicrosoftVisualCPlusPlusRedistributableExists = this.IsMicrosoftVisualCPlusPlus2017Available();
                if (File.Exists("cudnn64_7.dll"))
                {
                    report.CudnnExists = true;
                }

                System.Collections.IDictionary envirormentVariables = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
                if (envirormentVariables.Contains("CUDA_PATH"))
                {
                    report.CudaExists = true;
                }
                if (envirormentVariables.Contains("CUDA_PATH_V10_2"))
                {
                    report.CudaExists = true;
                }

                return report;
            }

            private bool IsMicrosoftVisualCPlusPlus2017Available()
            {
                //Detect if Visual C++ Redistributable for Visual Studio is installed
                //https://stackoverflow.com/questions/12206314/detect-if-visual-c-redistributable-for-visual-studio-2012-is-installed/
                Dictionary<string, string> checkKeys = new Dictionary<string, string>
            {
                { @"Installer\Dependencies\,,amd64,14.0,bundle", "Microsoft Visual C++ 2017 Redistributable (x64)" },
                { @"Installer\Dependencies\VC,redist.x64,amd64,14.16,bundle", "Microsoft Visual C++ 2017 Redistributable (x64)" },
                { @"Installer\Dependencies\VC,redist.x64,amd64,14.20,bundle", "Microsoft Visual C++ 2015-2019 Redistributable (x64)" },
                { @"Installer\Dependencies\VC,redist.x64,amd64,14.21,bundle", "Microsoft Visual C++ 2015-2019 Redistributable (x64)" },
                { @"Installer\Dependencies\VC,redist.x64,amd64,14.22,bundle", "Microsoft Visual C++ 2015-2019 Redistributable (x64)" },
                { @"Installer\Dependencies\VC,redist.x64,amd64,14.23,bundle", "Microsoft Visual C++ 2015-2019 Redistributable (x64)" },
                { @"Installer\Dependencies\VC,redist.x64,amd64,14.24,bundle", "Microsoft Visual C++ 2015-2019 Redistributable (x64)" },
                { @"Installer\Dependencies\VC,redist.x64,amd64,14.25,bundle", "Microsoft Visual C++ 2015-2019 Redistributable (x64)" },
                { @"Installer\Dependencies\VC,redist.x64,amd64,14.26,bundle", "Microsoft Visual C++ 2015-2019 Redistributable (x64)" },
                { @"Installer\Dependencies\VC,redist.x64,amd64,14.27,bundle", "Microsoft Visual C++ 2015-2019 Redistributable (x64)" },
                { @"Installer\Dependencies\VC,redist.x64,amd64,14.28,bundle", "Microsoft Visual C++ 2015-2019 Redistributable (x64)" },
                { @"Installer\Dependencies\VC,redist.x64,amd64,14.29,bundle", "Microsoft Visual C++ 2015-2019 Redistributable (x64)" }
            };

                foreach (KeyValuePair<string, string> checkKey in checkKeys)
                {
                    using (RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey(checkKey.Key, false))
                    {
                        if (registryKey == null)
                        {
                            continue;
                        }

                        string displayName = registryKey.GetValue("DisplayName") as string;
                        if (string.IsNullOrEmpty(displayName))
                        {
                            continue;
                        }

                        if (displayName.StartsWith(checkKey.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        #endregion

        public static readonly string YoloNames = Path.Combine("Assets", "coco.names");
        public static readonly string YoloConfig = Path.Combine("Assets", "yolov3.cfg");
        public static readonly string YoloWeights = Path.Combine("Assets", "yolov3.weights");

        private readonly YoloWrapper yolo;

        public DetectionSystem DetectionSystem => yolo.DetectionSystem;

        public AlturosYoloV3Detector()
        {
            AlturosValidator validator = new AlturosValidator();
            try
            {
                yolo = new YoloWrapper(YoloConfig, YoloWeights, YoloNames, new GpuConfig(), validator);
            }
            catch
            {
                yolo = new YoloWrapper(YoloConfig, YoloWeights, YoloNames, null, validator);
            }
        }

        public IEnumerable<(float Confidence, double CenterX, double CenterY, double Width, double Height)> Detect(Mat image)
        {
            List<(float Confidence, double CenterX, double CenterY, double Width, double Height)> items = new List<(float Confidence, double CenterX, double CenterY, double Width, double Height)>();
            foreach (YoloItem item in yolo.Detect(image.ToBytes()))
            {
                if (item.Type.Equals("person"))
                {
                    items.Add(((float)item.Confidence, item.X + item.Width / 2.0, item.Y + item.Height / 2.0, item.Width, item.Height));
                }
            }
            //return items;
            CvDnn.NMSBoxes(
                items.Select(i => new Rect((int)(i.CenterX - (i.Width / 2)), (int)(i.CenterY - (i.Height / 2)), (int)i.Width, (int)i.Height)),
                items.Select(i => i.Confidence),
                (float)Defaults.ConfidenceThreshold,
                (float)Defaults.NonMaximaSupressionThreshold,
                out int[] indices);
            for (int i = 0; i < indices.Length; i++)
            {
                yield return items[indices[i]];
            }
        }
    }
}
