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
using InTheHand.Net.Sockets;
using InTheHand.Net.Bluetooth;
using System.ComponentModel;

namespace TryDecrypt
{
    public class MainWindowViewModel : BaseViewModel
    {

        private byte[] defaultImage;
        private byte[] image1;
        private byte[] image2;
        private byte[] image3;

        public WriteableBitmap ViewImage { get; private set; }

        public string CodeText { get; set; } = "";

        public ICommand CCodeInputed { get; }

        public bool doFailureAni { get; set; } = false;
        public bool doSuccessAni { get; set; } = false;
        public bool doSolvedAni { get; set; } = false;
        public bool doCancelAni { get; set; } = false;

        public string EncryptText { get; private set; }

        public bool AllowInput { get; set; } = true;

        public BindingList<string> SeccessesNames { get; set; } = new BindingList<string>();
        private int[] bibleCode = new int[5];
        public List<string> nomalNames = new List<string>();
        private List<int> bibleCodes = new List<int>();

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
            currentData = new Random().Next(0, 5);
            EncryptText = encryptionData[currentData];

            readImage();

            makeNewCodes();
            makeSpecialCodes();

            bluetoothSetup();
        }

        // ============================ Setup ============================ 

        private void bluetoothSetup()
        {
            BluetoothClient bc = new BluetoothClient();
            BluetoothListener li = new BluetoothListener(BluetoothService.SerialPort);
            li.Start();

            new Thread((obj)=>send_controler((BluetoothListener)obj)).Start(li);
        }

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

            image3 = new byte[4 * 100 * 100];
            System.Runtime.InteropServices.Marshal.Copy(
                new WriteableBitmap(new BitmapImage(new Uri("image3.png", UriKind.Relative))).BackBuffer,
                image3,
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
                    doneList[i] = (data[i].CompareTo("True") == 0);
                for (int i = 0; i < 5; i++)
                    specialDoneList[i] = (data[5 + i].CompareTo("True") == 0);
                for (int i = 0; i < 5; i++)
                    bibleCode[i] = int.Parse(data[10 + i]);
                SeccessesNames.Clear();
                for (int i = 0; i < 5; i++)
                    SeccessesNames.Add(data[15+i]);
                nomalNames.Clear();
                bibleCodes.Clear();
                for (int i = 0; i < (data.Length - 20) / 2; i++)
                {
                    nomalNames.Add(data[20 + i * 2]);
                    bibleCodes.Add(int.Parse(data[20 + i * 2 + 1]));
                }
            }
            catch
            {
                for (int i = 0; i < doneList.Length; i++)
                    doneList[i] = false;
                for (int i = 0; i < specialDoneList.Length; i++)
                    specialDoneList[i] = false;
                for (int i = 0; i < 5; i++)
                    bibleCode[i] = -1;
                SeccessesNames.Clear();
                for (int i = 0; i < 5; i++)
                    SeccessesNames.Add("");
                nomalNames.Clear();
                bibleCodes.Clear();
            }
        }

        public void writeDataToFile()
        {
            string data = "";
            for (int i = 0; i < doneList.Length; i++)
                data = data + doneList[i] + "/";
            for (int i = 0; i < specialDoneList.Length; i++)
                data = data + specialDoneList[i] + "/";
            for (int i = 0; i < bibleCode.Length; i++)
                data = data + bibleCode[i] + "/";
            for (int i = 0; i < SeccessesNames.Count; i++)
                data = data + SeccessesNames[i] + "/";
            for (int i = 0; i < nomalNames.Count; i++)
                data = data + nomalNames[i] + "/" + bibleCodes[i] + "/";
            data = data.Substring(0, data.Length - 1);

            System.IO.FileStream f = new System.IO.FileStream(".\\data", System.IO.FileMode.Create);
            System.IO.StreamWriter w = new System.IO.StreamWriter(f);

            w.Write(data);

            w.Close();
            f.Close();

            f = new System.IO.FileStream(".\\data_"+ (DateTime.UtcNow.Ticks / 10000000), System.IO.FileMode.Create);
            w = new System.IO.StreamWriter(f);

            w.Write(data);

            w.Close();
            f.Close();
        }

        // ================================ BlueTooth Threading ==================================

        Mutex bluetoothMutex = new Mutex();

        Stack<string> sendTexts = new Stack<string>();

        private void addMessage(string s)
        {
            bluetoothMutex.WaitOne();
            sendTexts.Push(s + "\n");
            bluetoothMutex.ReleaseMutex();
        }

        byte[] buf = new byte[1000];
        private void receiver(System.Net.Sockets.Socket client)
        {
            int cnt;
            while (true)
            {
                cnt = client.Receive(buf);
                if (cnt == 0)
                    break;

                string msg = Encoding.UTF8.GetString(buf, 0, cnt);
                if (msg[0] == 'o')
                    Console.WriteLine("rec");
                if (msg[0] == 'i')
                    makeNewCodes();
                if (msg[0] == 'n')
                {
                    try
                    {
                        name = msg.Substring(2).Trim();
                    }
                    catch
                    {
                        denied = true;
                    }
                }
                if (msg[0] == 'd')
                    denied = true;
                if (msg[0] == 's')
                {
                    if (msg.Length > 1 && msg[1] == 'a')
                    {
                        writeDataToFile();
                    }
                    else
                    {
                        string ms = "";
                        for (int i = 0; i < 5; i++)
                            ms += "special " + (i + 1) + " : " + SeccessesNames[i] + (bibleCode[i] == -1? "" : " - " + decryptedData[bibleCode[i]] ) + "\n";
                        for (int i = 0; i < nomalNames.Count; i++)
                            ms += (i + 1) + " : " + nomalNames[i] + " - " + decryptedData[bibleCodes[i]] + "\n";
                        ms += "----------------";
                        addMessage(ms);
                    }
                }
                if (msg[0] == 'r')
                {
                    if (msg.Length > 1 && msg[1] == 'a')
                    {
                        for (int i = 0; i < SeccessesNames.Count; i++)
                        {
                            dispatcher.Invoke(()=>
                            SeccessesNames[i] = "");
                            specialDoneList[i] = false;
                            bibleCode[i] = -1;
                        }
                        nomalNames.Clear();
                        bibleCodes.Clear();
                    }
                    else if (msg.Length > 1 && msg[1] == 's')
                    {
                        int del = -1;
                        try { del = int.Parse(msg.Substring(2, msg.Length - 2)) - 1; } catch { }
                        if (del >= 0 && del < SeccessesNames.Count)
                        {
                            dispatcher.Invoke(() =>
                            SeccessesNames[del] = "");
                            specialDoneList[del] = false;
                            bibleCode[del] = -1;
                        }
                    }
                    else
                    {
                        int del = -1;
                        try { del = msg[2] - '0' - 1; } catch { }
                        if (del >= 0 && del <= 4)
                        {
                            nomalNames.RemoveAt(del);
                            bibleCodes.RemoveAt(del);
                        }
                    }
                }
            }
            recEnded = true;
        }

        bool recEnded = false;

        private void send_controler(BluetoothListener li)
        {
            while (true)
            {
                BluetoothClient cl = li.AcceptBluetoothClient();
                recEnded = false;
                new Thread(obj => receiver((System.Net.Sockets.Socket)obj)).Start(cl.Client);

                cl.Client.Send(Encoding.UTF8.GetBytes("=========================\ns : 상태보기\ni : 5개 일반암호 리셋\nr 숫자 : n번째 성공 지우기\nrs 숫자 : n번째 특수성공 지우기\nra : 모두 지우기\nsa : 저장하기\n=========================\n"));

                while (true)
                {
                    if (recEnded)
                        break;

                    bluetoothMutex.WaitOne();
                    if (sendTexts.Count != 0)
                    {
                        try
                        {
                            cl.Client.Send(Encoding.UTF8.GetBytes(sendTexts.Peek()));
                            sendTexts.Pop();
                        }
                        catch
                        {

                        }
                    }
                    bluetoothMutex.ReleaseMutex();
                }
            }
        }

        // ===========================================================================

        private string[][] codeBases =
        {
            new string[] {"FF", "6C" },
            new string[] { "F4", "C3", "57"},
            new string[] {"AE", "FE", "D9"},
            new string[] {"7B", "5D", "3E"}
        };

        private void makeNewCodes()
        {
            int[] randlist = new int[5];
            Random r = new Random();
            int randcnt = 0;
            while (randcnt < 5)
            {
                int num = 0;
                for (int i = 0; i < 3; i++)
                    num = num * 10 + r.Next(1, 4);
                bool isSame = false;
                for (int i = 0; i < randcnt; i++)
                    if (randlist[i] == num)
                    {
                        isSame = true;
                        break;
                    }

                if (!isSame)
                    randlist[randcnt++] = num;
            }

            codeList = new string[5];
            for (int i = 0; i < 5; i++)
            {
                codeList[i] = codeBases[1][randlist[i] / 100 % 10 - 1] + codeBases[2][randlist[i] / 10 % 10 - 1] + codeBases[3][randlist[i] % 10 - 1];
                doneList[i] = false;
            }
        }

        private void makeSpecialCodes() 
        {
            int[] randlist = { 1213, 1312, 1131, 2132, 2331 };

            specialCodeList = new string[5];
            for (int i = 0; i < 5; i++)
                specialCodeList[i] = codeBases[0][randlist[i] / 1000 % 10 - 1] + codeBases[1][randlist[i] / 100 % 10 - 1] + codeBases[2][randlist[i] / 10 % 10 - 1] + codeBases[3][randlist[i] % 10 - 1];
        }

        private string[] codeList;

        private string[] specialCodeList;

        private int currentData = 0;

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
            "사랑은 허다한 죄를 덮느니라",
            "두려워하지 말라 내가 너와 함께 함이라",
            "주의 말씀은 내 발에 등이요 내 길에 빛이니이다",
            "둘째도 그와 같으니 네 이웃을 네 자신 같이 사랑하라 하셨으니",
            "너는 마음을 다하여 여호와를 의뢰하고 네 명철을 의지하지 말라",
            "미움은 다툼을 일으켜도 사랑은 모든 허물을 가리우느니라"
        };

        private bool[] doneList =
        {
            false,
            false,
            false,
            false,
            false
        };

        private bool[] specialDoneList =
        {
            false,
            false,
            false,
            false,
            false
        };

        // ======================= Animation Controler ======================= 

        private void doFailAnimation()
        {
            doFailureAni = true;
            dispatcher.Invoke(() =>
            OnPropertyChanged(nameof(doFailureAni)));
            doFailureAni = false;
            dispatcher.Invoke(() =>
            OnPropertyChanged(nameof(doFailureAni)));
        }

        private void doCancelAnimation()
        {
            doCancelAni = true;
            dispatcher.Invoke(() =>
            OnPropertyChanged(nameof(doCancelAni)));
            doCancelAni = false;
            dispatcher.Invoke(() =>
            OnPropertyChanged(nameof(doCancelAni)));
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

        // ======================= Model Logic ======================= 

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
            for (int i = 0; i < specialCodeList.Length; i++)
                if (specialCodeList[i].CompareTo(CodeText.ToUpper().Trim()) == 0)
                {
                    // 특수 코드 성공
                    new Thread(obj => specialProcessor((int)obj)).Start(i);
                    return;
                }

            // 실패
            new Thread(obj => failProcessor()).Start();
        }

        bool denied = false;
        string name = null;

        private void successProcessor(int i)
        {
            int originlen = encryptionData[currentData].Length;
            int declen = decryptedData[i].Length;
            double donePercent;

            byte[] imageArray;
            if (doneList[i])
            {
                // 중복 성공
                addMessage("● 중복 성공");
                imageArray = image2;
                doSolvedAnimation();
            }
            else
            {
                // 미중복 성공
                addMessage("● 성공!!!");
                imageArray = image1;
                doSuccsessAnimation();
            }

            for (int idx = 0; idx < 100*2; idx++)
            {
                donePercent = (double)idx / (100 * 2 - 1);
                EncryptText =
                    decryptedData[i].Substring(0, (int)(donePercent * declen))
                    + encryptionData[currentData].Substring((int)(donePercent * originlen), originlen - (int)(donePercent * originlen));
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
            }
            else
            {
                // 미중복 성공
                // 등록 처리
                addMessage("   말씀 : " + decryptedData[i]);
                name = null;
                denied = false;
                addMessage("   이름을 등록해주세요 : n 이름 / d");
                while (name == null && !denied) ;
                if (denied)
                {
                    // 거부 처리
                    CodeText = "";
                    dispatcher.Invoke(()=>
                    OnPropertyChanged(nameof(CodeText)));
                    doCancelAnimation();

                    Thread.Sleep(500);
                }
                else
                {
                    // 이름과 현황 저장
                    nomalNames.Add(name);
                    bibleCodes.Add(i);
                    writeDataToFile();
                    doneList[i] = true;

                    bool isFull = true;
                    foreach (bool s in doneList)
                        if (!s)
                        {
                            isFull = false;
                            break;
                        }
                    if (isFull)
                        makeNewCodes();
                }

                // 복귀 처리
                closeSuccessAnimation();
            }

            // 복귀 처리
            byte[] a = new byte[4 * 50];
            Random value = new Random();
            currentData = value.Next(0, 5);
            originlen = encryptionData[currentData].Length;

            for (int idx = 0; idx < 100 * 2; idx++)
            {
                donePercent = (double)idx / (100 * 2 - 1);
                EncryptText =
                    encryptionData[currentData].Substring(0, (int)(donePercent * originlen))
                    + decryptedData[i].Substring((int)(donePercent * declen), declen - (int)(donePercent * declen));
                OnPropertyChanged(nameof(EncryptText));

                value.NextBytes(a);
                ViewImage.Dispatcher.Invoke(
                    new Action(() => ViewImage.WritePixels(new Int32Rect(idx % 2 * 50, idx / 2, 50, 1), a, 4 * 50, 0))
                    );

                Thread.Sleep(10);
            }

            AllowInput = true;
            OnPropertyChanged(nameof(AllowInput));
        }

        private void specialProcessor(int i)
        {
            int originlen = encryptionData[currentData].Length;
            int declen = decryptedData[i + 5].Length;
            double donePercent;

            byte[] imageArray;
            if (specialDoneList[i])
            {
                // 중복 성공
                addMessage("● 중복 특수코드 성공");
                imageArray = image2;
                doSolvedAnimation();
            }
            else
            {
                // 미중복 성공
                addMessage("● 특수코드 성공!!!");
                imageArray = image3;
                doSuccsessAnimation();
            }

            for (int idx = 0; idx < 100 * 2; idx++)
            {
                donePercent = (double)idx / (100 * 2 - 1);
                EncryptText =
                    decryptedData[i + 5].Substring(0, (int)(donePercent * declen))
                    + encryptionData[currentData].Substring((int)(donePercent * originlen), originlen - (int)(donePercent * originlen));
                OnPropertyChanged(nameof(EncryptText));

                ViewImage.Dispatcher.Invoke(
                    new Action(() => ViewImage.WritePixels(new Int32Rect(idx % 2 * 50, idx / 2, 50, 1), imageArray, 4 * 50, 4 * (idx % 2 * 50 + idx / 2 * 100)))
                    );

                Thread.Sleep(10);
            }

            if (specialDoneList[i])
            {
                // 중복 성공
                Thread.Sleep(1000);
            }
            else
            {
                // 미중복 성공
                // 등록 처리
                addMessage("   말씀 : " + decryptedData[i + 5]);
                name = null;
                denied = false;
                addMessage("   이름을 등록해주세요 : n 이름 / d");
                while (name == null && !denied) ;
                if (denied)
                {
                    // 거부 처리
                    CodeText = "";
                    dispatcher.Invoke(() =>
                    OnPropertyChanged(nameof(CodeText)));
                    doCancelAnimation();

                    Thread.Sleep(500);
                }
                else
                {
                    // 이름과 현황 저장
                    dispatcher.Invoke(() => { SeccessesNames.Insert(i, name); SeccessesNames.RemoveAt(i + 1); });
                    bibleCode[i] = i + 5;
                    writeDataToFile();
                    specialDoneList[i] = true;
                }

                // 복귀 처리
                closeSuccessAnimation();
            }

            // 복귀 처리
            byte[] a = new byte[4 * 50];
            Random value = new Random();
            currentData = value.Next(0, 5);
            originlen = encryptionData[currentData].Length;

            for (int idx = 0; idx < 100 * 2; idx++)
            {
                donePercent = (double)idx / (100 * 2 - 1);
                EncryptText =
                    encryptionData[currentData].Substring(0, (int)(donePercent * originlen))
                    + decryptedData[i + 5].Substring((int)(donePercent * declen), declen - (int)(donePercent * declen));
                OnPropertyChanged(nameof(EncryptText));

                value.NextBytes(a);
                ViewImage.Dispatcher.Invoke(
                    new Action(() => ViewImage.WritePixels(new Int32Rect(idx % 2 * 50, idx / 2, 50, 1), a, 4 * 50, 0))
                    );

                Thread.Sleep(10);
            }

            AllowInput = true;
            OnPropertyChanged(nameof(AllowInput));
        }

        private void failProcessor()
        {
            doFailAnimation();

            addMessage("● 실패!");

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
