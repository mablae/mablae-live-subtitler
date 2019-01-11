using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Speech.V1;
using Grpc.Core.Utils;
using NAudio.Wave;

namespace Mablae.LiveSubtitler
{
    public class Transcriber
    {
        private WaveInEvent waveIn;
        private CancellationTokenSource tokenCancelEarly;
        private SpeechClient speechClient;
        private string lastString;

        public Transcriber(WaveInEvent waveInEvent, SpeechClient speechClient)
        {
            waveIn =
                waveInEvent;
            this.speechClient = speechClient;
        } 
        
        public async Task<int> Run(int seconds, string locale)
        {
            tokenCancelEarly = new CancellationTokenSource();

            
            var streamingCall = speechClient.StreamingRecognize();


  
                

            // Write the initial request with the config.
             await streamingCall.WriteAsync(
                new StreamingRecognizeRequest()
                {
                    StreamingConfig = new StreamingRecognitionConfig()
                    {
                        Config = new RecognitionConfig()
                        {
                            Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                            SampleRateHertz = 16000,
                            LanguageCode = locale,

                            EnableAutomaticPunctuation = true,
                        },
                        InterimResults = true,
                        SingleUtterance = true
                    }
                });
            
            
            
            // Print responses as they arrive.
            Task printResponses = Task.Run(async () =>
            {
                Console.WriteLine("Start Print");
                while (await streamingCall.ResponseStream.MoveNext(default(CancellationToken)))
                {
                    var error = streamingCall.ResponseStream.Current.Error;
                    if (error != null)
                    {
                        Console.WriteLine(String.Format("Code: {0} \"{1}\"", error.Code, error.Message), Color.Red);
                        tokenCancelEarly.Cancel();
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        return;

                    }
                    
                    foreach (var result in streamingCall.ResponseStream.Current.Results)
                    {
                        if (result.Stability > 0.8)
                        {
                            foreach (var alternative in result.Alternatives)
                            {
                                if (lastString != alternative.Transcript)
                                {
                                //    Console.WriteLine(alternative.Transcript);
                                //    Console.SetCursorPosition(0, Console.CursorTop - 1);
                                    
                                    PartialTranscriptionReceivedEventArgs args = new PartialTranscriptionReceivedEventArgs();
                                    args.Transcription = alternative.Transcript;
                                    OnPartialTranscriptionReceived(args);

                                }


                                lastString = alternative.Transcript;
                            }
                        }

                        if (result.IsFinal != true) continue;
                        foreach (var alternative in result.Alternatives)
                        {
                          
                            Console.WriteLine(alternative.Transcript);
                            
                            CompleteTranscriptionReceivedEventArgs args = new CompleteTranscriptionReceivedEventArgs();
                            args.Transcription = alternative.Transcript;
                            OnCompleteTranscriptionReceived(args);
                            
                            
                            tokenCancelEarly.Cancel();
                        }
                    }
                }
                
                Console.WriteLine("End Print");
            });
            
            
            // Read from the microphone and stream to API.
            object writeLock = new object();
            bool writeMore = true;
            waveIn.DataAvailable +=
                (object sender, NAudio.Wave.WaveInEventArgs waveInEventArgs) =>
                {
                    lock (writeLock)
                    {
                        if (!writeMore) return;
                        if (streamingCall != null)
                        {
                            streamingCall.WriteAsync(
                                new StreamingRecognizeRequest()
                                {
                                    AudioContent = Google.Protobuf.ByteString
                                        .CopyFrom(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded)
                                }).Wait();
                        }
                    }
                };


            try
            {
                await Task.Delay(TimeSpan.FromSeconds(seconds), tokenCancelEarly.Token);
            }
            catch (TaskCanceledException e)
            {
                
            }
            finally
            {
                lock (writeLock) writeMore = false;
                await streamingCall.WriteCompleteAsync();
            }

            
            // Stop recording and shut down.
            await printResponses;
            
            
            return 0;
        }
        
        
        
        protected virtual void OnPartialTranscriptionReceived(PartialTranscriptionReceivedEventArgs e)
        {
            EventHandler<PartialTranscriptionReceivedEventArgs> handler = PartialTranscriptionReceived;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler<PartialTranscriptionReceivedEventArgs> PartialTranscriptionReceived;
        
        
        protected virtual void OnCompleteTranscriptionReceived(CompleteTranscriptionReceivedEventArgs e)
        {
            EventHandler<CompleteTranscriptionReceivedEventArgs> handler = CompleteTranscriptionReceived;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler<CompleteTranscriptionReceivedEventArgs> CompleteTranscriptionReceived;

    }
    
     
    public class CompleteTranscriptionReceivedEventArgs : EventArgs
    {
        public string Transcription { get; set; }
        
    }
    
    public class PartialTranscriptionReceivedEventArgs : EventArgs
    {
        public string Transcription { get; set; }
        
    }
    
}