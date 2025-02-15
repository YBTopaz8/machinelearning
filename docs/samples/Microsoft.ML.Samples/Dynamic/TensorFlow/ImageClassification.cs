﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Samples.Dynamic
{
    public static class ImageClassification
    {
        /// <summary>
        /// Example use of the TensorFlow image model in a ML.NET pipeline.
        /// </summary>
        public static void Example()
        {
            // Download the ResNet 101 model from the location below.
            // https://storage.googleapis.com/download.tensorflow.org/models/tflite_11_05_08/resnet_v2_101.tgz

            string modelLocation = "resnet_v2_101_299_frozen.pb";
            if (!File.Exists(modelLocation))
            {
                modelLocation = Download(@"https://storage.googleapis.com/download.tensorflow.org/models/tflite_11_05_08/resnet_v2_101.tgz", @"resnet_v2_101_299_frozen.tgz");
                Unzip(Path.Join(Directory.GetCurrentDirectory(), modelLocation),
                    Directory.GetCurrentDirectory());

                modelLocation = "resnet_v2_101_299_frozen.pb";
            }

            var mlContext = new MLContext();
            var data = GetTensorData();
            var idv = mlContext.Data.LoadFromEnumerable(data);

            // Create a ML pipeline.
            using var model = mlContext.Model.LoadTensorFlowModel(modelLocation);
            var pipeline = model.ScoreTensorFlowModel(
                new[] { nameof(OutputScores.output) },
                new[] { nameof(TensorData.input) }, addBatchDimensionInput: true);

            // Run the pipeline and get the transformed values.
            var estimator = pipeline.Fit(idv);
            var transformedValues = estimator.Transform(idv);

            // Retrieve model scores.
            var outScores = mlContext.Data.CreateEnumerable<OutputScores>(
                transformedValues, reuseRowObject: false);

            // Display scores. (for the sake of brevity we display scores of the
            // first 3 classes)
            foreach (var prediction in outScores)
            {
                int numClasses = 0;
                foreach (var classScore in prediction.output.Take(3))
                {
                    Console.WriteLine(
                        $"Class #{numClasses++} score = {classScore}");
                }
                Console.WriteLine(new string('-', 10));
            }

            // Results look like below...
            //Class #0 score = -0.8092947
            //Class #1 score = -0.3310375
            //Class #2 score = 0.1119193
            //----------
            //Class #0 score = -0.7807726
            //Class #1 score = -0.2158062
            //Class #2 score = 0.1153686
            //----------
        }

        private const int imageHeight = 224;
        private const int imageWidth = 224;
        private const int numChannels = 3;
        private const int inputSize = imageHeight * imageWidth * numChannels;

        /// <summary>
        /// A class to hold sample tensor data. 
        /// Member name should match the inputs that the model expects (in this
        /// case, input).
        /// </summary>
        public class TensorData
        {
            [VectorType(imageHeight, imageWidth, numChannels)]
            public float[] input { get; set; }
        }

        /// <summary>
        /// Method to generate sample test data. Returns 2 sample rows.
        /// </summary>
        public static TensorData[] GetTensorData()
        {
            // This can be any numerical data. Assume image pixel values.
            var image1 = Enumerable.Range(0, inputSize).Select(
                x => (float)x / inputSize).ToArray();

            var image2 = Enumerable.Range(0, inputSize).Select(
                x => (float)(x + 10000) / inputSize).ToArray();
            return new TensorData[] { new TensorData() { input = image1 },
                new TensorData() { input = image2 } };
        }

        /// <summary>
        /// Class to contain the output values from the transformation.
        /// </summary>
        class OutputScores
        {
            public float[] output { get; set; }
        }

        private static string Download(string baseGitPath, string dataFile)
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(new Uri($"{baseGitPath}"), dataFile);
            }

            return dataFile;
        }

        /// <summary>
        /// Taken from 
        /// https://github.com/icsharpcode/SharpZipLib/wiki/GZip-and-Tar-Samples.
        /// </summary>
        private static void Unzip(string path, string targetDir)
        {
            Stream inStream = File.OpenRead(path);
            Stream gzipStream = new GZipInputStream(inStream);

            TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream, Encoding.ASCII);
            tarArchive.ExtractContents(targetDir);
            tarArchive.Close();

            gzipStream.Close();
            inStream.Close();
        }
    }
}
