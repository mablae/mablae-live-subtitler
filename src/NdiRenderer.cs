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
        private int frameNumber = 0;
        private CancellationToken cancellationToken;
        public string PartialText = "";
        public string TranslatedText = "";

        public NdiRenderer(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
        }

        static void DrawPrettyText(Graphics graphics, String text, float size, FontFamily family, Point origin, StringFormat format, Brush fill, Pen outline)
        {
            // make a text path
            GraphicsPath path = new GraphicsPath();
            path.AddString(text, family, 0, size, origin, format);

            // Draw the pretty text
            graphics.FillPath(fill, path);
            graphics.DrawPath(outline, path);
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
                // We are going to create a 1920x1080 16:9 frame at 29.97Hz, progressive (default).
                // We are also going to create an audio frame with enough for 1700 samples for a bit of safety,
                // but 1602 should be enough using our settings as long as we don't overrun the buffer.
                // 48khz, stereo in the example.
                using (VideoFrame videoFrame = new VideoFrame(1920, 1080, (16.0f / 9.0f), 30000, 1001))
               
                {
                    // get a compatible bitmap and graphics context from our video frame.
                    // also sharing a using scope.
                    using (Bitmap bmp = new Bitmap(videoFrame.Width, videoFrame.Height, videoFrame.Stride, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, videoFrame.BufferPtr))
                    using (Graphics graphics = Graphics.FromImage(bmp))
                    {
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;

                        // We'll use these later inside the loop
                        StringFormat textFormat = new StringFormat();
                        textFormat.Alignment = StringAlignment.Near;
                        textFormat.LineAlignment = StringAlignment.Center;

                        FontFamily fontFamily = new FontFamily("Arial");
                        Font font = new Font(fontFamily, 36.0f, FontStyle.Regular, GraphicsUnit.Pixel);
                        Pen outlinePen = new Pen(Color.Black, 1.0f);
                        Pen thinOutlinePen = new Pen(Color.Black, 1.0f);

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
                

                                // fill it with a lovely color
                                graphics.Clear(Color.Transparent);

                                // show which source we are
                             //   DrawPrettyText(graphics, "C# Example Source", 96.0f, fontFamily, new Point(960, 100), textFormat, Brushes.White, outlinePen);

                                // Get the tally state of this source (we poll it),
                                // This gets a snapshot of the current tally state.
                                // Accessing sendInstance.Tally directly would make an API call
                                // for each "if" below and could cause inaccurate results.
                                NDIlib.tally_t NDI_tally = sendInstance.Tally;

                                // Do something different depending on where we are shown
                              /*
                               * if (NDI_tally.on_program)
                               
                                    DrawPrettyText(graphics, "On Program", 96.0f, fontFamily, new Point(960, 225), textFormat, Brushes.White, outlinePen);
                                else if (NDI_tally.on_preview)
                                    DrawPrettyText(graphics, "On Preview", 96.0f, fontFamily, new Point(960, 225), textFormat, Brushes.White, outlinePen);
                                */
                                //// show what frame we've rendered
                   //             DrawPrettyText(graphics, String.Format("Frame {0}", frameNumber.ToString()), 96.0f, fontFamily, new Point(960, 350), textFormat, Brushes.White, outlinePen);

                                // show current time
                              //  DrawPrettyText(graphics, PartialText, 36.0f, fontFamily, new Point(10, 1000), textFormat, Brushes.White, outlinePen);

                          
                                RectangleF transpileRect = new RectangleF(10, videoFrame.Height - 130, videoFrame.Width - 20 , 120);
                                graphics.DrawString(PartialText, font, Brushes.White, transpileRect);
                                
                                
                                RectangleF translationRect = new RectangleF(10, videoFrame.Height - 250, videoFrame.Width - 20 , 120);
                                graphics.DrawString(TranslatedText, font, Brushes.YellowGreen, translationRect);

              
                                // We now submit the frame. Note that this call will be clocked so that we end up submitting 
                                // at exactly 29.97fps.
                                sendInstance.Send(videoFrame);

                                // Just display something helpful in the console
                              //  Console.WriteLine("Frame number {0} sent.", frameNumber);

                                frameNumber++;
                            }

                        } // for loop - frameNumber

                    } // using bmp and graphics

                } // using audioFrame and videoFrame

            } // using sendInstance

            return 0;
        } // Main


    }
}