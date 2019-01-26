﻿using Google.Cloud.Speech.V1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class ASRJStreamingWithFile
    {
        //static string defInputFilePath = @"F:\IFMWork_ASRTTS\BoceraniTrascrizioni\_VOC_35977945.pcm000.pcm";
        static string defInputFilePath = @"F:\IFMWork_ASRTTS\TTS_IVONA_Carla.wav";

        static int Main(string[] args)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", @"F:\IFMWork_ASRTTS\ASRTTS_Cloud\ASRAziendale-a2226fbd6a6f.json");
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", @"F:\IFMWork_ASRTTS\ASRAziendale-a2226fbd6a6f.json");

            Console.WriteLine("Test ASR Google By Joba!!!");
            
            string inputFile;
            if (args.Count() < 1)
            {
                Array.Resize(ref args, 20);

                inputFile = defInputFilePath;
            }
            else
            {
                inputFile = args[0];
            }

            var t = StreamingRecognizeAsync(inputFile, 22050);

            try
            {
                 t.Wait();
            }
            catch (AggregateException ex)
            {
               Console.WriteLine($"AggregateException{ex.ToString()}");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"AggregateException{ex.ToString()}");
            }
              
            Console.WriteLine("Test ASR Google Terminated!!!");

            return 0;
        }

        static async Task<object> StreamingRecognizeAsync(string filePath, int freqsamp = 8000)
        {
            var speech = SpeechClient.Create();
            var streamingCall = speech.StreamingRecognize();
            // Write the initial request with the config.
            await streamingCall.WriteAsync(
                new StreamingRecognizeRequest()
                {
                    StreamingConfig = new StreamingRecognitionConfig()
                    {
                        Config = new RecognitionConfig()
                        {
                            Encoding =
                            RecognitionConfig.Types.AudioEncoding.Linear16,
                            SampleRateHertz = freqsamp,
                            LanguageCode = "it",
                        },
                        InterimResults = true,
                    }
                });
            // Print responses as they arrive.
            Task printResponses = Task.Run(async () =>
            {
                while (await streamingCall.ResponseStream.MoveNext(
                    default(CancellationToken)))
                {
                    foreach (var result in streamingCall.ResponseStream
                        .Current.Results)
                    {
                        foreach (var alternative in result.Alternatives)
                        {
                            Console.WriteLine(alternative.Transcript);
                        }
                    }
                }
            });
            // Stream the file content to the API.  Write 2 32kb chunks per
            // second.
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var buffer = new byte[32 * 1024];
                int bytesRead;
                while ((bytesRead = await fileStream.ReadAsync(
                    buffer, 0, buffer.Length)) > 0)
                {
                    await streamingCall.WriteAsync(
                        new StreamingRecognizeRequest()
                        {
                            AudioContent = Google.Protobuf.ByteString
                            .CopyFrom(buffer, 0, bytesRead),
                        });
                    await Task.Delay(500);
                };
            }
            await streamingCall.WriteCompleteAsync();
            await printResponses;
            return 0;
        }
    }
}
