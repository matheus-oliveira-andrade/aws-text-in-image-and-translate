using Amazon;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.Translate;
using Amazon.Translate.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AWSTextInImageAndTranslate
{
    public partial class FrmMain : Form
    {
        private AWSCredentials _awsCredentials;
        private readonly RegionEndpoint _region = RegionEndpoint.USEast1;
        private const string BUCKET_NAME = "aws-text-in-image-translate";

        public FrmMain()
        {
            InitializeComponent();

            comboBox1.DataSource = new BindingSource(GetLanguages(), null);
            comboBox1.DisplayMember = "Key";
            comboBox1.ValueMember = "Value";

        }

        private async void BtnSelecionar_Click(object sender, EventArgs e)
        {

            try
            {
                DialogResult result = openFileDialog1.ShowDialog();

                if (result == DialogResult.OK)
                {
                    pictureBox1.Load(openFileDialog1.FileName);

                    await UploadImageToS3(openFileDialog1.FileName);
                    List<string> detections = await DetectTextInImage(openFileDialog1.FileName);

                    richTextBox1.Clear();
                    richTextBox2.Clear();

                    foreach (string detection in detections)
                    {
                        if (!string.IsNullOrEmpty(detection))
                        {
                            string selectedLanguage = comboBox1.SelectedValue.ToString() ?? "en";

                            string translatedText = await TranslateText(detection, selectedLanguage);

                            // text not translated
                            richTextBox2.AppendText(detection + Environment.NewLine);

                            // text transalated
                            richTextBox1.AppendText(translatedText + Environment.NewLine);
                        }
                    }

                    if (detections.Count <= 0)
                        MessageBox.Show("Not recognized text in image");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async Task<string> TranslateText(string text, string language)
        {
            var client = new AmazonTranslateClient(_awsCredentials, _region);
            TranslateTextRequest request = new TranslateTextRequest()
            {
                Text = text,
                SourceLanguageCode = "auto",
                TargetLanguageCode = language,
            };

            TranslateTextResponse result = await client.TranslateTextAsync(request);

            return result.TranslatedText;
        }

        private async Task<List<string>> DetectTextInImage(string fileName)
        {
            var rekognitionClient = new AmazonRekognitionClient(_awsCredentials, _region);
            var detectTextRequest = new DetectTextRequest()
            {
                Image = new Amazon.Rekognition.Model.Image()
                {
                    S3Object = new S3Object()
                    {
                        Name = Path.GetFileName(fileName),
                        Bucket = BUCKET_NAME
                    }
                }
            };

            DetectTextResponse detectTextResponse = await rekognitionClient.DetectTextAsync(detectTextRequest);

            List<string> detections = detectTextResponse.TextDetections.Where(td => td.Confidence > 80 && td.Type == TextTypes.LINE).Select(td => td.DetectedText).ToList();

            foreach (TextDetection textDetection in detectTextResponse.TextDetections.Where(td => td.Type == TextTypes.WORD))
            {
                Pen pen = new Pen(Color.Yellow);

                Graphics graphics = pictureBox1.CreateGraphics();

                var boundingBox = textDetection.Geometry.BoundingBox;
                var imageDimensions = pictureBox1.Image;

                graphics.DrawRectangle(pen,
                                       boundingBox.Left * imageDimensions.Width,
                                       boundingBox.Top * imageDimensions.Height,
                                       boundingBox.Width * imageDimensions.Width,
                                       boundingBox.Height * imageDimensions.Height);
            }

            return detections;
        }

        private async Task<bool> UploadImageToS3(string file)
        {
            GetCredentials();

            try
            {
                AmazonS3Client s3Client = new AmazonS3Client(_awsCredentials, _region);
                TransferUtility fileTransferUtility = new TransferUtility(s3Client);

                await fileTransferUtility.UploadAsync(file, BUCKET_NAME);

                return true;
            }
            catch (AmazonS3Exception ex)
            {
                MessageBox.Show($"Error encountered on server. Message: '{0}' when writing an object {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unknown encountered on server. Message: '{0}' when writing an object {ex.Message}");
                return false;
            }
        }

        private void GetCredentials()
        {
            var chain = new CredentialProfileStoreChain();

            if (!chain.TryGetAWSCredentials("AWS Educate profile", out _awsCredentials))
            {
                MessageBox.Show("Error fetching credentials");
            }
        }

        private Dictionary<string, string> GetLanguages()
        {
            return new Dictionary<string, string>
            {
                { "Arabic", "ar" },
                { "Armenian", "hy" },
                { "Azerbaijani", "az" },
                { "Bengali", "bn" },
                { "Bosnian", "bs" },
                { "Bulgarian", "bg" },
                { "Catalan", "ca" },
                { "Chinese (Simplified)", "zh" },
                { "Chinese (Traditional)", "zh-TW" },
                { "Croatian", "hr" },
                { "Czech", "cs" },
                { "Danish", "da" },
                { "Dari", "fa-AF" },
                { "Dutch", "nl" },
                { "English", "en" },
                { "Finnish", "fi" },
                { "French", "fr" },
                { "French (Canada)", "fr-CA" },
                { "Hindi", "hi" },
                { "Hungarian", "hu" },
                { "Icelandic", "is" },
                { "Indonesian", "id" },
                { "Italian ", "it" },
                { "Japanese", "ja" },
                { "Korean", "ko" },
                { "Portuguese", "pt" },
                { "Slovak", "sk" },
                { "Somali", "so" },
                { "Spanish", "es" },
                { "Spanish (Mexico)", "es-MX"},
            };
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
