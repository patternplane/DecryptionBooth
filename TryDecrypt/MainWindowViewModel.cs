using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace TryDecrypt
{
    public class MainWindowViewModel : BaseViewModel
    {

        private byte[] defaultImage;
        private byte[] image1;
        private byte[] image2;

        public WriteableBitmap ViewImage { get; private set; }

        public string CodeText { get; set; } = "";

        public ICommand CCodeInputed { get; }

        public bool doFailureAni { get; set; } = false;
        public bool doSuccessAni { get; set; } = false;
        public bool doSolvedAni { get; set; } = false;

        public string EncryptText { get; private set; }

        public bool AllowInput { get; set; } = true;

        private Dispatcher dispatcher;

        public MainWindowViewModel()
        {
            dispatcher = Dispatcher.CurrentDispatcher;

            PixelFormat pf = PixelFormats.Bgr32;
            int width = 100;
            int height = 100;
            int rawStride = (width * pf.BitsPerPixel + 7) / 8;

            defaultImage = new byte[rawStride * height];
            Random value = new Random();
            value.NextBytes(defaultImage);

            ViewImage = new WriteableBitmap(width, height,
                96, 96, pf, null);
            ViewImage.WritePixels(new Int32Rect(0, 0, width, height), defaultImage, rawStride, 0);

            CCodeInputed = new RelayCommand(obj => codeInputed());

            readSavedFile();
            for (int i = 0; i < doneList.Length; i++) 
                if (!doneList[i])
                {
                    EncryptText = encryptionData[i];
                    break;
                }

            readImage();

            /*
            PixelFormat pf = PixelFormats.Bgr32;
            int width = 10;
            int height = 10;
            int rawStride = (width * pf.BitsPerPixel + 7) / 8;

            Random value = new Random();
            for (int i = 0; i < 10; i++)
            {
                value.NextBytes(rawImage);
                Thread.Sleep(500);
                bitmap2.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => {
                    bitmap2.WritePixels(new Int32Rect(0, 0, 10, 10), rawImage, rawStride, 0);
                }));
            }*/
        }

        // ============================ model processes ============================ 

        private void readImage()
        {
            image1 = new byte[4 * 100 * 100];
            System.Runtime.InteropServices.Marshal.Copy(
                new WriteableBitmap(new BitmapImage(new Uri("image1.png", UriKind.Relative))).BackBuffer,
                image1,
                0,
                4 * 100 * 100);

            image2 = new byte[4 * 100 * 100];
            System.Runtime.InteropServices.Marshal.Copy(
                new WriteableBitmap(new BitmapImage(new Uri("image2.png", UriKind.Relative))).BackBuffer,
                image2,
                0,
                4 * 100 * 100);
        }

        private void readSavedFile()
        {
            try
            {
                System.IO.FileStream f = new System.IO.FileStream(".\\data", System.IO.FileMode.Open);
                System.IO.StreamReader r = new System.IO.StreamReader(f);
                string[] data = r.ReadToEnd().Split('/');
                r.Close();
                f.Close();

                for (int i = 0; i < 5; i++)
                    doneList[i] = (data[i].CompareTo("true") == 0);
                doneCount = int.Parse(data[5]);
            } 
            catch
            {
                for (int i = 0; i < doneList.Length; i++)
                    doneList[i] = false;
            }
        }

        public void writeDataToFile()
        {
            string data = "";
            for (int i = 0; i < doneList.Length; i++)
                data = data + doneList[i] + "/";
            data = data + doneCount;

            System.IO.FileStream f = new System.IO.FileStream(".\\data", System.IO.FileMode.CreateNew);
            System.IO.StreamWriter w = new System.IO.StreamWriter(f);

            w.Write(data);

            w.Close();
            f.Close();
        }

        private string[] codeList =
        {
            "F4FE5D",
            "C3AE3E",
            "57D97B",
            "C3FE3E",
            "57AE7B"
        };

        private string[] encryptionData =
        {
            "7KO87J2YIO2BrOqzoCDsi6DruYTtlZwg6rOE7ZqN7J2EIOuIhOqwgCDslYzrnrQ=",
            "N0lLMDdKV0U2ck9FN0l1Z0lPeWp2T3VsdkNEc3NLenNs",
            "cEh0bFpqcm5id2c2NGFBNjUyODdKcTBJT3lqdkE9PQ==",
            "N0tPODdKMllJT3lkdE91bWhPeWRoQ0RxdUxEc2xyWHRsWmpy",
            "aXBRZzdKNlFJT3V6dGV5ZHRDRHNub2pyajRUcmk2UT0="
        };

        private string[] decryptedData =
        {
            "우리가 사랑함은 그가 먼저 우리를 사랑하셨음이라",
            "경건에 형제 우애를, 형제 우애에 사랑을 더하라",
            "서로 돌아보아 사랑과 선행을 격려하며",
            "이 모든 것 위에 사랑을 더하라 이는 온전하게 매는 띠니라",
            "사랑은 허다한 죄를 덮느니라"
        };

        private int doneCount = 0;

        private bool[] doneList =
        {
            false,
            false,
            false,
            false,
            false
        };

        private void doFailAnimation()
        {
            doFailureAni = true;
            dispatcher.Invoke(() =>
            OnPropertyChanged(nameof(doFailureAni)));
            doFailureAni = false;
            dispatcher.Invoke(() =>
            OnPropertyChanged(nameof(doFailureAni)));
        }

        private void doSuccsessAnimation()
        {
            doSuccessAni = true;
            OnPropertyChanged(nameof(doSuccessAni));
        }

        private void closeSuccessAnimation()
        {
            doSuccessAni = false;
            OnPropertyChanged(nameof(doSuccessAni));
        }

        private void doSolvedAnimation()
        {
            doSolvedAni = true;
            dispatcher.Invoke(() =>
            OnPropertyChanged(nameof(doSolvedAni)));
            doSolvedAni = false;
            dispatcher.Invoke(() =>
            OnPropertyChanged(nameof(doSolvedAni)));
        }

        private void codeInputed()
        {
            AllowInput = false;
            OnPropertyChanged(nameof(AllowInput));

            for (int i = 0; i < codeList.Length; i++)
                if (codeList[i].CompareTo(CodeText.ToUpper().Trim()) == 0)
                {
                    // 성공
                    new Thread(obj => successProcessor((int)obj)).Start(i);
                    return;
                }

            // 실패
            new Thread(obj => failProcessor()).Start();
        }

        private void successProcessor(int i)
        {
            int originlen = encryptionData[i].Length;
            int declen = decryptedData[i].Length;
            double donePercent;

            byte[] imageArray;
            if (doneList[i])
            {
                // 중복 성공
                imageArray = image2;
                doSolvedAnimation();
            }
            else
            {
                // 미중복 성공
                imageArray = image1;
                doSuccsessAnimation();
            }

            for (int idx = 0; idx < 100*2; idx++)
            {
                donePercent = (double)idx / (100 * 2 - 1);
                EncryptText =
                    decryptedData[i].Substring(0, (int)(donePercent * declen))
                    + encryptionData[i].Substring((int)(donePercent * originlen), originlen - (int)(donePercent * originlen));
                OnPropertyChanged(nameof(EncryptText));

                ViewImage.Dispatcher.Invoke(
                    new Action(() => ViewImage.WritePixels(new Int32Rect(idx % 2 * 50, idx / 2, 50, 1), imageArray, 4*50, 4*(idx % 2 * 50 + idx / 2 * 100)))
                    );

                Thread.Sleep(10);
            }

            if (doneList[i])
            {
                // 중복 성공
                Thread.Sleep(1000);

                // 복귀 처리
            }
            else
            {
                // 미중복 성공
                // 등록 처리

                // 복귀 처리
                closeSuccessAnimation();
            }

            doneList[i] = true;
            AllowInput = true;
            OnPropertyChanged(nameof(AllowInput));
        }

        private void failProcessor()
        {
            doFailAnimation();

            byte[] a = new byte[4 * 50];
            Random value = new Random();

            for (int idx = 0; idx < 100 * 2; idx++)
            {
                value.NextBytes(a);
                ViewImage.Dispatcher.Invoke(
                    new Action(() => ViewImage.WritePixels(new Int32Rect(idx % 2 * 50, idx / 2, 50, 1), a, 4 * 50, 0))
                    );

                Thread.Sleep(10);
            }

            AllowInput = true;
            OnPropertyChanged(nameof(AllowInput));
        }
    }
}
