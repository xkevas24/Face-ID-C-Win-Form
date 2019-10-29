using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;
using System.Net;
//using System.Net.Sockets;
//using AForge;
//using AForge.Video;
using AForge.Video.DirectShow;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
//using System.ServiceProcess;

namespace VideoWorkDemo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //初始化
        FilterInfoCollection videoDevices;
        VideoCaptureDevice videoSource;

        private void Form1_Load(object sender, EventArgs e)
        {
            //int i = 0;
            //VideoWork wv = new VideoWork(panel1.Handle, 0, 0, panel1.Width, panel1.Height);
            //wv.Start();
           
            try
            {
                // 枚举所有视频输入设备
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videoDevices.Count == 0)
                    throw new ApplicationException();

                foreach (FilterInfo device in videoDevices)
                {
                    comboBox1.Items.Add(device.Name);
                }

                comboBox1.SelectedIndex = 0;

            }
            catch (ApplicationException)
            {
                comboBox1.Items.Add("No local capture devices");
                videoDevices = null;
            }

            CameraConn();
            label_name.Text = "";
            label_sex.Text = "";
            label_stuno.Text = "";
            label_class.Text = "";
            infolog.Text = "设备初始化完成";
            infolog.AutoSize = true;
            label_workdir.Text = "";
            label_workdir.Text = Application.StartupPath;
            label_url.Text = "";
            //检测photos文件夹是否存在，否则创建
            if (Directory.Exists(Application.StartupPath + "/photos/") == false)//如果不存
            {
                Directory.CreateDirectory(Application.StartupPath + "/photos/");
            }
            label_photos.Text = Application.StartupPath + @"\photos\";

            //操作IIS
            label_http.Text= Application.StartupPath + @"\photos\";
            string sv = GetIpAddress();
            byte[] postdata = Encoding.UTF8.GetBytes("");
            //Info_PHP("http://" + sv, postdata);
            label9.Text = "本地IP：" + sv;
        }

        private void CameraConn()
        {
            try
            {
                videoSource = new VideoCaptureDevice(videoDevices[comboBox1.SelectedIndex].MonikerString);
            }
            catch
            {
                MessageBox.Show("请先连接视频捕获设备！程序即将强制退出！");
                Process.GetCurrentProcess().Kill();
            }
            
            //videoSource.DesiredFrameSize = new Size(320, 240);
            //videoSource.DesiredFrameRate = 1;

            videoSourcePlayer1.VideoSource = videoSource;
            videoSourcePlayer1.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            videoSourcePlayer1.SignalToStop();
            videoSourcePlayer1.WaitForStop();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Shoot();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Shoot();
            //button1_Click(sender,e);
        }

        public void Shoot()
        {
            if (videoSource == null)
                return;
            Bitmap bitmap = videoSourcePlayer1.GetCurrentVideoFrame();
            string fileName = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ff") + ".jpg";
            bitmap.Save(Application.StartupPath + "/photos/" + fileName, ImageFormat.Jpeg);
            pic.ImageLocation = Application.StartupPath + "/photos/" + fileName;
            bitmap.Dispose();
            //ImgToBase64String(pic.ImageLocation);
            //string image_b64=System.IO.File.ReadAllText(pic.ImageLocation);
            //string image_b64 = ImgToBase64String(bitmap);
            //byte[] image = Encoding.UTF8.GetBytes("photo=" + image_b64);
            //string image_b64 = Convert.ToBase64String(imageToByteArray(pic.ImageLocation));
            byte[] image = Encoding.UTF8.GetBytes("photo=" + fileName);
            string sv = GetIpAddress();
            var resstring = Info_PHP("https://wlfw.ynudcc.cn/topstu/faceid/client_image_post.php?sv="+sv,image);
            //MessageBox.Show(resstring);
            if (resstring != "")
            {
                xjzp.ImageLocation = "http://wlfw.ynudcc.cn/app/student/photos_get_nosession.php?stuno=" + resstring;
                //拉取学籍信息
                byte[] postdata = Encoding.UTF8.GetBytes("");
                var stuinfo = Info_PHP("https://wlfw.ynudcc.cn/topstu/faceid/client_stuinfo.php?stuno="+resstring,postdata);
                var recInfo = JsonConvert.DeserializeObject<Rec_Info>(stuinfo);
                string name = recInfo.name;
                string sex = recInfo.sex;
                string bjmc = recInfo.bjmc;
                //MessageBox.Show(stuinfo);
                label_name.Text = name;
                label_sex.Text = sex;
                label_stuno.Text = resstring;
                label_class.Text = bjmc;
                infolog.Text = "成功检索到人脸信息";
                Wav("10_success.wav");

            }
            else
            {
                label_name.Text = "";
                label_sex.Text = "";
                label_stuno.Text = "";
                label_class.Text = "";
                infolog.Text = resstring;
                xjzp.ImageLocation = "";
                
               infolog.Text = "未检测到匹配的人脸信息";
                //Wav("03_hint.wav");
            }
            label_url.Text = "http://" + sv + "/" + fileName;
        }

        //播放音频文件
        private void Play_Voice(string address)
        {
            try
            {
                SoundPlayer player = new SoundPlayer();
                player.SoundLocation = address;
                player.Load(); //同步加载声音  
                player.Play(); //启用新线程播放  
                player.PlayLooping(); //循环播放模式  
                player.PlaySync(); //UI线程同步播放
            }
            catch (Exception)
            {
                MessageBox.Show("音频缺失！");
                //throw;
            }

        }

        public void Wav(string list)
        {
            Play_Voice(Application.StartupPath + @"\" + list);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            Wav("03_hint.wav");
            button2.Visible=false;
        }
        //与PHP后台交互
        private string Info_PHP(string url, byte[] postdata)
        {
            try
            {
                WebClient wc = new WebClient();
                wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                byte[] postData = postdata;
                byte[] responseData = wc.UploadData(url, "POST", postData); // 得到返回字符流
                var resstring = Encoding.UTF8.GetString(responseData);
                wc.Dispose();
                return resstring;
            }
            catch (Exception)
            {
                MessageBox.Show("连接超时，网络故障！程序将自动退出，请检查网络正常后再开启此程序！");
                System.Environment.Exit(0);
                throw new Exception("连接超时");
            }


        }

        private string GetIpAddress()
        {
            string hostName = Dns.GetHostName();   //获取本机名
            IPHostEntry localhost = Dns.GetHostByName(hostName);    //方法已过期，可以获取IPv4的地址
                                                                    //IPHostEntry localhost = Dns.GetHostEntry(hostName);   //获取IPv6地址
            IPAddress localaddr = localhost.AddressList[0];

            return localaddr.ToString();
        }

        private void button_config_Click(object sender, EventArgs e)
        {
            int timer_sec;
            int.TryParse(text_timer.Text, out timer_sec);
            timer1.Enabled = false;
            timer1.Interval = 1000 * timer_sec;
            timer1.Enabled = true;
            MessageBox.Show("操作完成！");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Process.Start("iisreset");
            MessageBox.Show("操作完成！");
        }

        public static void DelectDir(string srcPath)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(srcPath);
                FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();  //返回目录中所有文件和子目录
                foreach (FileSystemInfo i in fileinfo)
                {
                    if (i is DirectoryInfo)            //判断是否文件夹
                    {
                        DirectoryInfo subdir = new DirectoryInfo(i.FullName);
                        subdir.Delete(true);          //删除子目录和文件
                    }
                    else
                    {
                        File.Delete(i.FullName);      //删除指定文件
                    }
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DelectDir(Application.StartupPath + "/photos/");
            MessageBox.Show("缓存删除成功！");
        }

        private void timer_end_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            MessageBox.Show("进程已终止！");
        }
    }
}
