using NewTek;
using NewTek.NDI;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;

namespace Mablae.LiveSubtitler
{
    public class NdiRenderer
    {
        private CancellationToken cancellationToken;
        private string partialText = "";
        private string translatedText = "";
        private readonly VideoFrame videoFrame;
        private readonly Font font;
        private readonly RectangleF transpileRect;
        private readonly RectangleF translationRect;
        private Bitmap bmp;
        private Graphics graphics;
        private bool allowSending;

        public NdiRenderer(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;

            // We are going to create a 1920x1080 16:9 frame at 29.97Hz, progressive (default).
            this.videoFrame = new VideoFrame(1920, 1080, (16.0f / 9.0f), 30000, 1001);

            font = new Font(new FontFamily("Arial"), 36.0f, FontStyle.Regular, GraphicsUnit.Pixel);
            transpileRect = new RectangleF(10, videoFrame.Height - 130, videoFrame.Width - 20, 120);
            translationRect = new RectangleF(10, videoFrame.Height - 250, videoFrame.Width - 20, 120);

            bmp = new Bitmap(videoFrame.Width, videoFrame.Height, videoFrame.Stride,
                System.Drawing.Imaging.PixelFormat.Format32bppPArgb, videoFrame.BufferPtr);
            graphics = Graphics.FromImage(bmp);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
        }


        public async Task<int> UpdatePartialText(string newPartialText)
        {
            partialText = newPartialText;
            DrawFrame();

            return 0;
        }

        public async Task<int> UpdateTranslatedText(string updatedTranslationText)
        {
            translatedText = updatedTranslationText;
            DrawFrame();

            return 0;
        }

        private void DrawFrame()
        {
            allowSending = false;
            graphics.Clear(Color.Transparent);
            graphics.DrawString(partialText, font, Brushes.White, transpileRect);
            graphics.DrawString(translatedText, font, Brushes.YellowGreen, translationRect);

            allowSending = true;
        }

        public async Task<int> Run()
        {
            // Note that some of these using statements are sharing the same scope and
            // will be disposed together simply because I dislike deeply nested scopes.
            // You can manually handle .Dispose() for longer lived objects or use any pattern you prefer.

            // When creating the sender use the Managed NDIlib Send example as the failover for this sender
            // Therefore if you run both examples and then close this one it demonstrates failover in action
            String failoverName = String.Format("{0} (NDIlib Send Example)", System.Net.Dns.GetHostName());

            // this will show up as a source named "Example" with all other settings at their defaults
            using (Sender sendInstance = new Sender("Example", true, false, null, failoverName))
            {
                // We will send 10000 frames of video.
                while (!cancellationToken.IsCancellationRequested)
                {
                    // are we connected to anyone?
                    if (sendInstance.GetConnections(10000) < 1)
                    {
                        // no point rendering
                        Console.WriteLine("No current connections, so no rendering needed.");

                        // Wait a bit, otherwise our limited example will end before you can connect to it
                        System.Threading.Thread.Sleep(50);
                    }
                    else
                    {
                        // We now submit the frame. Note that this call will be clocked so that we end up submitting at exactly 29.97fps.
                        if (allowSending)
                        {
                            sendInstance.Send(videoFrame);
                        }
                    }
                } // using sendInstance

                return 0;
            } // Main
        }
    }
}