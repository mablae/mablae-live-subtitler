using System;
using System.Threading.Tasks;
using Google.Cloud.Translation.V2;
using Console = Colorful.Console;


namespace Mablae.LiveSubtitler
{
    public class Translator
    {
        private readonly TranslationClient client;

        public Translator()
        {
            
            this.client = TranslationClient.Create();
        }
        
        public async Task<int> Translate(string toTranslate, string sourceLanguage, string targetLanguage     )
        {
            Task<TranslationResult> response =  Task.Run(async () => await client.TranslateTextAsync(toTranslate, targetLanguage, sourceLanguage));

            await response;
            var args = new TranslationReceivedEventArgs {Translation = response.Result.TranslatedText};
            
            Console.WriteLine(args.Translation);
            OnTranslationReceived(args);
            
            return 0;
        }
        
        
        protected virtual void OnTranslationReceived(TranslationReceivedEventArgs e)
        {
            var handler = TranslationReceived;
            handler?.Invoke(this, e);
        }

        public event EventHandler<TranslationReceivedEventArgs> TranslationReceived;
    }
    
    public class TranslationReceivedEventArgs : EventArgs
    {
        public string Translation { get; set; }
        
    }
}