//using System;
//using Microsoft.ML.Probabilistic.Models;
//using Microsoft.ML.Probabilistic.Models.Attributes;
//using Microsoft.ML.Probabilistic.Distributions;
//using Microsoft.ML.Probabilistic.Algorithms;
//using Microsoft.ML.Probabilistic.Math;
//using Microsoft.ML.Probabilistic.Compiler;
//using Microsoft.ML.Probabilistic.Compiler.Transforms;
//using Microsoft.ML.Probabilistic.Compiler.Visualizers;
//using Range = Microsoft.ML.Probabilistic.Models.Range;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Diagnostics;
//using System.Runtime.InteropServices;
//using System.IO;

//namespace TestInfer
//{
//    class Program
//    {
//        static void Main(string[] args)
//        {
//            /**************************************************************************************************************
//             Notes:
//               (1) This script takes as input data from two csv files (without headers or index columns):
//                   (1.1) the file "data.csv" should contain all the connections between customers and sites over all days.
//                         Each row is a jagged array of unique site ids that a customer connected to.
//                   (1.2) the file "nps.csv" should contain all nps scores per customer.
//               (2) The number of days should be set by the variable "numDays" accordingly.
//               (3) The unique number of sites should be set by the variable "numUniqueSites" accordingly.
//               (4) After inference completed, the site performance probabilities are output to a file called "results.csv".
//                   Results are reshaped into a 2D array, where rows indicate days, and columns indicate unique sites.
//            ***************************************************************************************************************/

//            // the data.csv file contains all the customer site connections for all days.
//            string fileName = "19-01-2021-datasets/interactions.csv";
//            string[] lines = File.ReadAllLines(fileName);
//            int[][] sitesIndicesForEachCustomer = new int[lines.Length][];
//            int totNumSiteNodes = 0;

//            for (int i = 0; i < lines.Length; i++)
//            {
//                string[] strArray = lines[i].Split(';');
//                // remove empty null elements
//                strArray = strArray.Except(new List<string> { string.Empty }).ToArray();
//                //Console.WriteLine(strArray.Length);

//                int[] intArray = Array.ConvertAll(strArray, int.Parse);
//                sitesIndicesForEachCustomer[i] = intArray;

//                // find max site id in all the data (this indicates total number of site nodes for all days)
//                if (intArray.Max() > totNumSiteNodes)
//                    totNumSiteNodes = intArray.Max();
//            }

//            int numDays = 242; // set by user (121, 242)
//            int numUniqueSites = 1724; // set by user (1583, 1724)
//            int numSites = numDays * numUniqueSites;
//            int numCustomers = lines.Length;

//            Console.WriteLine("--------------------------------------------------------");
//            Console.WriteLine("Maximum site id found: {0}, total array size: {1}", totNumSiteNodes, numSites);
//            Console.WriteLine("Number of days: {0}", numDays);
//            Console.WriteLine("Number of unique sites: {0}", numUniqueSites);
//            Console.WriteLine("Total number of customers (from data file): {0}", numCustomers);

//            // get the number of site connections per customer
//            int[] numSitesForEachCustomer = new int[numCustomers];
//            for (int n = 0; n < numCustomers; n++)
//            {
//                numSitesForEachCustomer[n] = sitesIndicesForEachCustomer[n].Length;
//            }

//            // the nps.csv file contains all the customer nps scores.
//            fileName = "19-01-2021-datasets/responses.csv";
//            lines = File.ReadAllLines(fileName);
//            bool[] isDetractorAnswers = new bool[lines.Length];
//            //bool[] isMissingDetractor = new bool[lines.Length];

//            for (int i = 0; i < lines.Length; i++)
//            {
//                string[] strArray = lines[i].Split('|');
//                int[] intArray = Array.ConvertAll(strArray, int.Parse);

//                //if (intArray[0] == 100000)
//                //{
//                //    isMissingDetractor[i] = true;
//                //}
//                //else
//                //{
//                // TODO: we will need to think carefully about the value of this threshold
//                if (intArray[1] == 1)
//                {
//                    isDetractorAnswers[i] = true;
//                }
//                else
//                {
//                    isDetractorAnswers[i] = false;
//                }

//                //isMissingDetractor[i] = false;
//                //}

//                //Console.WriteLine("{0}: {1}", isDetractorAnswers[i], intArray[1]);

//            }

//            // the kpi.csv file contains all the network kpis.
//            fileName = "19-01-2021-datasets/dataset-kpis.csv";
//            lines = File.ReadAllLines(fileName);
//            double[] netKPIs = new double[numSites];
//            bool[] isMissing = new bool[numSites];
//            Vector[] netKPIsVector = new Vector[numSites];

//            for (int i = 0; i < numSites; i++)
//            {
//                string[] strArray = lines[i].Split(';');
//                double[] doubleArray = Array.ConvertAll(strArray, double.Parse);
//                double[] doubleArray2 = new double[5];

//                if (doubleArray[0] == 100000 | doubleArray[1] == 100000 | doubleArray[2] == 100000 | doubleArray[3] == 100000)
//                {
//                    isMissing[i] = true;
//                }
//                else
//                {
//                    isMissing[i] = false;
//                    doubleArray2[0] = doubleArray[0];
//                    doubleArray2[1] = doubleArray[1];
//                    doubleArray2[2] = doubleArray[2];
//                    doubleArray2[3] = doubleArray[3];
//                    doubleArray2[4] = 1.0;
//                    //doubleArray2[0] = doubleArray[2];
//                    //doubleArray2[1] = doubleArray[3];
//                    //netKPIsVector[i] = Vector.FromArray(doubleArray2);
//                    netKPIsVector[i] = Vector.FromArray(doubleArray2);
//                }
//            }

//            // the labels.csv file contains all the semi-supervised labels.
//            fileName = "19-01-2021-datasets/labels.csv";
//            lines = File.ReadAllLines(fileName);
//            bool[] hasLabel = new bool[numSites];
//            bool[] label = new bool[numSites];

//            for (int i = 0; i < numSites; i++)
//            {
//                string[] strArray = lines[i].Split(';');
//                double[] doubleArray = Array.ConvertAll(strArray, double.Parse);

//                if (doubleArray[0] == 100000)
//                {
//                    hasLabel[i] = false;
//                }
//                else
//                {
//                    hasLabel[i] = true;

//                    if (doubleArray[0] == 1)
//                    {
//                        label[i] = true;
//                    }
//                    else
//                    {
//                        label[i] = false;
//                    }
//                }
//            }

//            Console.WriteLine("Total number of customers (from nps file): {0}", isDetractorAnswers.Length);

//            Range rangeSites = new Range(numSites);
//            Range rangeCustomers = new Range(numCustomers);

//            VariableArray<int> numberOfsitesForEachCustomer = Variable.Array<int>(rangeCustomers).Named("numSitesForCustomers").Attrib(new DoNotInfer());
//            Range customersSites = new Range(numberOfsitesForEachCustomer[rangeCustomers]).Named("customersSites");

//            VariableArray<VariableArray<int>, int[][]> sitesTouched = Variable.Array(Variable.Array<int>(customersSites), rangeCustomers).Named("sitesTouched").Attrib(new DoNotInfer());

//            VariableArray<bool> site = Variable.Array<bool>(rangeSites).Named("sites");
//            //site[rangeSites] = Variable.Bernoulli(0.5).ForEach(rangeSites);
//            //VariableArray<bool> z = Variable.Array<bool>(rangeSites).Named("z");

//            //VariableArray<bool> siteLabel = Variable.Array<bool>(rangeSites).Named("sitesLabel");
//            //site[rangeSites] = Variable.Bernoulli(0.5).ForEach(rangeSites);
//            //siteLabel[rangeSites] = Variable.Bernoulli(0.5).ForEach(rangeSites);

//            VariableArray<bool> isDetractor = Variable.Array<bool>(rangeCustomers).Named("isDetractor");
//            //VariableArray<bool> isDetractorLabel = Variable.Array<bool>(rangeCustomers).Named("isDetractorLabel");
//            //isDetractorLabel[rangeCustomers] = Variable.Bernoulli(0.5).ForEach(rangeCustomers);

//            VariableArray<bool> hadBadSiteInt = Variable.Array<bool>(rangeCustomers).Named("hadBadSiteInt");

//            // TODO: think carefully about this prior
//            //VariableArray<double> trueDetractor = Variable.Array<double>(rangeCustomers).Named("trueDetractor");
//            //trueDetractor[rangeCustomers] = Variable.Beta(5, 2).ForEach(rangeCustomers);
//            //VariableArray<double> falseDetractor = Variable.Array<double>(rangeCustomers).Named("falseDetractor");
//            //falseDetractor[rangeCustomers] = Variable.Beta(2, 5).ForEach(rangeCustomers);
//            Variable<double> trueDetractor = Variable.Beta(5, 2).Named("trueDetractor");
//            Variable<double> falseDetractor = Variable.Beta(2, 5).Named("falseDetractor");

//            //// the Gaussian mixture part of the model
//            //Range k = new Range(2);
//            //VariableArray<Vector> means = Variable.Array<Vector>(k).Named("means");

//            //means[0] = Variable.VectorGaussianFromMeanAndPrecision(Vector.FromArray(-50.0, -5.0), PositiveDefiniteMatrix.IdentityScaledBy(2, 0.1));
//            //means[1] = Variable.VectorGaussianFromMeanAndPrecision(Vector.FromArray(-100.0, -10.0), PositiveDefiniteMatrix.IdentityScaledBy(2, 0.1));
//            //VariableArray<PositiveDefiniteMatrix> precisions = Variable.Array<PositiveDefiniteMatrix>(k).Named("precisions");
//            //precisions[k] = Variable.WishartFromShapeAndScale(10.0, PositiveDefiniteMatrix.IdentityScaledBy(2, 1)).ForEach(k);

//            //VariableArray<double> kpis = Variable.Array<double>(rangeSites).Named("kpis");
//            VariableArray<Vector> kpis = Variable.Array<Vector>(rangeSites).Named("kpis").Attrib(new DoNotInfer());

//            VariableArray<bool> isMissingVar = Variable.Observed(isMissing, rangeSites);

//            //Variable<double> weights = Variable.Beta(1, 1).Named("weights");

//            Variable<Vector> W = Variable.VectorGaussianFromMeanAndPrecision(Vector.Zero(5), PositiveDefiniteMatrix.IdentityScaledBy(5, 0.01)).Named("wVectorGaussian");

//            Variable<double> threshold = Variable.GaussianFromMeanAndPrecision(0, 1).Named("threshold");

//            double noise = 1.0 / 10.0;
//            VariableArray<double> score = Variable.Array<double>(rangeSites).Named("score");

//            using (var block = Variable.ForEach(rangeSites))
//            {
//                using (Variable.If(isMissingVar[rangeSites]))
//                {
//                    site[rangeSites] = Variable.Bernoulli(0.5);
//                    score[rangeSites] = Variable.GaussianFromMeanAndPrecision(0, 1);
//                }
//                using (Variable.IfNot(isMissingVar[rangeSites]))
//                {
//                    score[rangeSites] = Variable.GaussianFromMeanAndPrecision(Variable.InnerProduct(W, kpis[rangeSites]), noise);
//                    site[rangeSites] = score[rangeSites] > threshold;
//                }

//            }



//            //// create the Markov chain
//            //using (var block = Variable.ForEach(rangeSites))
//            //{
//            //    var t = block.Index;
//            //    var tIsGr = (t > (numUniqueSites - 1));

//            //    using (Variable.If(isMissingVar[t]))
//            //    {
//            //        site[t] = Variable.Bernoulli(0.5);
//            //    }

//            //    using (Variable.IfNot(isMissingVar[t]))
//            //    {
//            //        site[t] = Variable.Bernoulli(weights);

//            //        using (Variable.If(site[t]))
//            //        {
//            //            kpis[t] = Variable.VectorGaussianFromMeanAndPrecision(means[1], precisions[1]);
//            //        }
//            //        using (Variable.IfNot(site[t]))
//            //        {
//            //            kpis[t] = Variable.VectorGaussianFromMeanAndPrecision(means[0], precisions[0]);
//            //        }
//            //    }
//            //}

//            using (Variable.ForEach(rangeCustomers))
//            {
//                var relevantSites = Variable.Subarray(site, sitesTouched[rangeCustomers]).Named("relevantSites");

//                //create the allTrue factor
//                //hadBadSiteInt[rangeCustomers] = Variable.AllTrue(relevantSites).Named("allTrue");

//                //create the AnyTrue factor
//                var notRelevantSites = Variable.Array<bool>(customersSites);
//                notRelevantSites[customersSites] = !relevantSites[customersSites];
//                hadBadSiteInt[rangeCustomers] = !Variable.AllTrue(notRelevantSites).Named("anyTrue");

//                // add noise factor for isDetractor observations
//                using (Variable.If(hadBadSiteInt[rangeCustomers]))
//                {
//                    // TODO: we will need to think carefully here.
//                    //       TP: you had a bad network experience, and is a detractor. 
//                    //isDetractor[rangeCustomers].SetTo(Variable.Bernoulli(0.9));
//                    isDetractor[rangeCustomers].SetTo(Variable.Bernoulli(trueDetractor));
//                    //isDetractor[rangeCustomers].SetTo(Variable.Bernoulli(trueDetractor[rangeCustomers]));
//                }
//                using (Variable.IfNot(hadBadSiteInt[rangeCustomers]))
//                {
//                    // TODO: we will need to think carefully here.
//                    //       FP: you had a good network experience, but is a detractor.
//                    //isDetractor[rangeCustomers].SetTo(Variable.Bernoulli(0.1));
//                    isDetractor[rangeCustomers].SetTo(Variable.Bernoulli(falseDetractor));
//                    //isDetractor[rangeCustomers].SetTo(Variable.Bernoulli(falseDetractor[rangeCustomers]));
//                }

//            }

//            /********* observations *********/
//            isDetractor.ObservedValue = isDetractorAnswers;
//            //npsScore.ObservedValue = npsAnswers;
//            numberOfsitesForEachCustomer.ObservedValue = numSitesForEachCustomer;
//            sitesTouched.ObservedValue = sitesIndicesForEachCustomer;
//            kpis.ObservedValue = netKPIsVector;
//            //siteLabel.ObservedValue = label;
//            /*******************************/

//            /********** inference **********/
//            var InferenceEngine = new InferenceEngine(new ExpectationPropagation());
//            //var InferenceEngine = new InferenceEngine(new VariationalMessagePassing());
//            InferenceEngine.NumberOfIterations = 20;
//            //InferenceEngine.ShowFactorGraph = true;

//            Bernoulli[] sitesPosteriors = InferenceEngine.Infer<Bernoulli[]>(site);
//            VectorGaussian wPosterior = InferenceEngine.Infer<VectorGaussian>(W);
//            Gaussian postThreshold = InferenceEngine.Infer<Gaussian>(threshold);
//            //Gamma postThresholdPrecision = engine.Infer<Gamma>(thresholdPrecision);
//            Bernoulli[] hadBadPosteriors = InferenceEngine.Infer<Bernoulli[]>(hadBadSiteInt);

//            Beta trueDetractorPosteriors = InferenceEngine.Infer<Beta>(trueDetractor);
//            Beta falseDetractorPosteriors = InferenceEngine.Infer<Beta>(falseDetractor);

//            Console.WriteLine("True detractor: {0}", trueDetractorPosteriors);
//            Console.WriteLine("False detractor: {0}", falseDetractorPosteriors);

//            Console.WriteLine("Dist over W = " + wPosterior);
//            Console.WriteLine("Dist over threshold mean = " + postThreshold);

//            //for (int i = 0; i < numCustomers; i++)
//            //{
//            //    Console.WriteLine("H:{0} - D:{1}", hadBadPosteriors[i].GetProbTrue(), isDetractorAnswers[i]);
//            //}




//            //Bernoulli[] hadBadPosteriors = InferenceEngine.Infer<Bernoulli[]>(hadBadSiteInt);
//            //Beta trueDetractorPosteriors = InferenceEngine.Infer<Beta>(trueDetractor);
//            //Beta falseDetractorPosteriors = InferenceEngine.Infer<Beta>(falseDetractor);

//            //Beta[] falseSiteBadPosteriors = InferenceEngine.Infer<Beta[]>(falseSiteBad);
//            //Beta[] trueSiteBadPosteriors = InferenceEngine.Infer<Beta[]>(trueSiteBad);

//            //Beta weightsPosteriors = InferenceEngine.Infer<Beta>(weights);
//            //VectorGaussian[] meansPosteriors = InferenceEngine.Infer<VectorGaussian[]>(means);
//            //Wishart[] precisionsPosteriors = InferenceEngine.Infer<Wishart[]>(precisions);
//            //Gaussian postThresholdMean = InferenceEngine.Infer<Gaussian>(thresholdMean);
//            ///*******************************/

//            //for (int i = 0; i < 2; i++)
//            //{
//            //    Console.WriteLine("Posterior Gaussian: {0}", meansPosteriors[i]);
//            //    Console.WriteLine("Posterior Gamma: {0}", precisionsPosteriors[i].GetMean().Inverse());
//            //}

//            ////Console.WriteLine(postThresholdMean);
//            ////Console.WriteLine(postThresholdMean);

//            //Console.WriteLine("True detractor: {0}", trueDetractorPosteriors);
//            //Console.WriteLine("False detractor: {0}", falseDetractorPosteriors);
//            //Console.WriteLine("Weights: {0}", weightsPosteriors);

//            ///*********** outputs ***********/
//            //var storeWeights = new StringBuilder();
//            //var line = string.Format("{0}", weightsPosteriors.GetMean());
//            //storeWeights.AppendLine(line);

//            var storeSites = new StringBuilder();

//            for (int i = 0; i < numSites; i++)
//            {
//                var line = string.Format("{0}", sitesPosteriors[i].GetProbTrue());
//                storeSites.AppendLine(line);
//            }

//            //var storeCustomer = new StringBuilder();

//            //for (int i = 0; i < numCustomers; i++)
//            //{
//            //    line = string.Format("{0}", hadBadPosteriors[i].GetProbTrue());
//            //    storeCustomer.AppendLine(line);
//            //}

//            //var storeGMM = new StringBuilder();

//            //for (int i = 0; i < 2; i++)
//            //{
//            //    var meanVec = meansPosteriors[i].GetMean();
//            //    var varMat = precisionsPosteriors[i].GetMean().Inverse();
//            //    var newLine = string.Format("{0},{1},{2},{3},{4},{5}", meanVec[0], meanVec[1], varMat[0], varMat[1], varMat[2], varMat[3]);
//            //    //var newLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19}", meanVec[0], meanVec[1], meanVec[2], meanVec[3], varMat[0], varMat[1], varMat[2], varMat[3], varMat[4], varMat[5], varMat[6], varMat[7], varMat[8], varMat[9], varMat[10], varMat[11], varMat[12], varMat[13], varMat[14], varMat[15]);
//            //    storeGMM.AppendLine(newLine);
//            //}

//            File.WriteAllText("sites-results.csv", storeSites.ToString());
//            //File.WriteAllText("customer-results.csv", storeCustomer.ToString());
//            //File.WriteAllText("gmm-results.csv", storeGMM.ToString());
//            //File.WriteAllText("weights-results.csv", storeWeights.ToString());
//        }
//    }
//}

using System;
using Microsoft.ML.Probabilistic.Models;
using Microsoft.ML.Probabilistic.Models.Attributes;
using Microsoft.ML.Probabilistic.Distributions;
using Microsoft.ML.Probabilistic.Algorithms;
using Microsoft.ML.Probabilistic.Math;
using Microsoft.ML.Probabilistic.Compiler;
using Microsoft.ML.Probabilistic.Compiler.Transforms;
using Microsoft.ML.Probabilistic.Compiler.Visualizers;
using Range = Microsoft.ML.Probabilistic.Models.Range;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace TestInfer
{
    class Program
    {
        static void Main(string[] args)
        {
            /**************************************************************************************************************
             Notes:
               (1) This script takes as input data from two csv files (without headers or index columns):
                   (1.1) the file "data.csv" should contain all the connections between customers and sites over all days.
                         Each row is a jagged array of unique site ids that a customer connected to.
                   (1.2) the file "nps.csv" should contain all nps scores per customer.
               (2) The number of days should be set by the variable "numDays" accordingly.
               (3) The unique number of sites should be set by the variable "numUniqueSites" accordingly.
               (4) After inference completed, the site performance probabilities are output to a file called "results.csv".
                   Results are reshaped into a 2D array, where rows indicate days, and columns indicate unique sites.
            ***************************************************************************************************************/

            // the data.csv file contains all the customer site connections for all days.
            string fileName = "19-01-2021-datasets/interactions.csv";
            string[] lines = File.ReadAllLines(fileName);
            int[][] sitesIndicesForEachCustomer = new int[lines.Length][];
            int totNumSiteNodes = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string[] strArray = lines[i].Split(';');
                // remove empty null elements
                strArray = strArray.Except(new List<string> { string.Empty }).ToArray();
                //Console.WriteLine(strArray.Length);

                int[] intArray = Array.ConvertAll(strArray, int.Parse);
                sitesIndicesForEachCustomer[i] = intArray;

                // find max site id in all the data (this indicates total number of site nodes for all days)
                if (intArray.Max() > totNumSiteNodes)
                    totNumSiteNodes = intArray.Max();
            }

            int numDays = 242; // set by user (121, 242)
            int numUniqueSites = 1724; // set by user (1583, 1724)
            int numSites = numDays * numUniqueSites;
            int numCustomers = lines.Length;

            Console.WriteLine("--------------------------------------------------------");
            Console.WriteLine("Maximum site id found: {0}, total array size: {1}", totNumSiteNodes, numSites);
            Console.WriteLine("Number of days: {0}", numDays);
            Console.WriteLine("Number of unique sites: {0}", numUniqueSites);
            Console.WriteLine("Total number of customers (from data file): {0}", numCustomers);

            // get the number of site connections per customer
            int[] numSitesForEachCustomer = new int[numCustomers];
            for (int n = 0; n < numCustomers; n++)
            {
                numSitesForEachCustomer[n] = sitesIndicesForEachCustomer[n].Length;
            }

            // the nps.csv file contains all the customer nps scores.
            fileName = "19-01-2021-datasets/responses.csv";
            lines = File.ReadAllLines(fileName);
            bool[] isDetractorAnswers = new bool[lines.Length];
            //bool[] isMissingDetractor = new bool[lines.Length];

            for (int i = 0; i < lines.Length; i++)
            {
                string[] strArray = lines[i].Split('|');
                int[] intArray = Array.ConvertAll(strArray, int.Parse);

                //if (intArray[0] == 100000)
                //{
                //    isMissingDetractor[i] = true;
                //}
                //else
                //{
                // TODO: we will need to think carefully about the value of this threshold
                if (intArray[1] == 1)
                {
                    isDetractorAnswers[i] = true;
                }
                else
                {
                    isDetractorAnswers[i] = false;
                }

                //isMissingDetractor[i] = false;
                //}

                //Console.WriteLine("{0}: {1}", isDetractorAnswers[i], intArray[1]);

            }

            // the kpi.csv file contains all the network kpis.
            fileName = "19-01-2021-datasets/dataset-kpis.csv";
            lines = File.ReadAllLines(fileName);
            double[] netKPIs = new double[numSites];
            bool[] isMissing = new bool[numSites];
            Vector[] netKPIsVector = new Vector[numSites];

            for (int i = 0; i < numSites; i++)
            {
                string[] strArray = lines[i].Split(';');
                double[] doubleArray = Array.ConvertAll(strArray, double.Parse);
                //double[] doubleArray2 = new double[2];

                if (doubleArray[0] == 100000 | doubleArray[1] == 100000 | doubleArray[2] == 100000 | doubleArray[3] == 100000)
                {
                    isMissing[i] = true;
                }
                else
                {
                    isMissing[i] = false;
                    //doubleArray2[0] = doubleArray[2];
                    //doubleArray2[1] = doubleArray[3];
                    //netKPIsVector[i] = Vector.FromArray(doubleArray2);
                    netKPIsVector[i] = Vector.FromArray(doubleArray);
                }
            }

            // the labels.csv file contains all the semi-supervised labels.
            fileName = "19-01-2021-datasets/labels.csv";
            lines = File.ReadAllLines(fileName);
            bool[] hasLabel = new bool[numSites];
            bool[] label = new bool[numSites];

            int p = 0;

            for (int i = 0; i < numSites; i++)
            {
                string[] strArray = lines[i].Split(';');
                double[] doubleArray = Array.ConvertAll(strArray, double.Parse);

                if (doubleArray[0] == 100000)
                {
                    hasLabel[i] = false;
                }
                else
                {
                    hasLabel[i] = true;
                    p++;

                    if (doubleArray[0] == 1)
                    {
                        label[i] = true;
                    }
                    else
                    {
                        label[i] = false;
                    }
                }
            }

            Console.WriteLine(p);

            Console.WriteLine("Total number of customers (from nps file): {0}", isDetractorAnswers.Length);

            Range rangeSites = new Range(numSites);
            Range rangeCustomers = new Range(numCustomers);

            VariableArray<int> numberOfsitesForEachCustomer = Variable.Array<int>(rangeCustomers).Named("numSitesForCustomers").Attrib(new DoNotInfer());
            Range customersSites = new Range(numberOfsitesForEachCustomer[rangeCustomers]).Named("customersSites");

            VariableArray<VariableArray<int>, int[][]> sitesTouched = Variable.Array(Variable.Array<int>(customersSites), rangeCustomers).Named("sitesTouched").Attrib(new DoNotInfer());

            VariableArray<bool> site = Variable.Array<bool>(rangeSites).Named("sites");
            //site[rangeSites] = Variable.Bernoulli(0.5).ForEach(rangeSites);
            //VariableArray<bool> z = Variable.Array<bool>(rangeSites).Named("z");

            VariableArray<bool> siteLabel = Variable.Array<bool>(rangeSites).Named("sitesLabel");
            //site[rangeSites] = Variable.Bernoulli(0.5).ForEach(rangeSites);
            siteLabel[rangeSites] = Variable.Bernoulli(0.5).ForEach(rangeSites);

            VariableArray<bool> isDetractor = Variable.Array<bool>(rangeCustomers).Named("isDetractor");
            //VariableArray<bool> isDetractorLabel = Variable.Array<bool>(rangeCustomers).Named("isDetractorLabel");
            //isDetractorLabel[rangeCustomers] = Variable.Bernoulli(0.5).ForEach(rangeCustomers);

            VariableArray<bool> hadBadSiteInt = Variable.Array<bool>(rangeCustomers).Named("hadBadSiteInt");

            // TODO: think carefully about this prior
            //VariableArray<double> trueDetractor = Variable.Array<double>(rangeCustomers).Named("trueDetractor");
            //trueDetractor[rangeCustomers] = Variable.Beta(5, 2).ForEach(rangeCustomers);
            //VariableArray<double> falseDetractor = Variable.Array<double>(rangeCustomers).Named("falseDetractor");
            //falseDetractor[rangeCustomers] = Variable.Beta(2, 5).ForEach(rangeCustomers);
            Variable<double> trueDetractor = Variable.Beta(5, 2).Named("trueDetractor");
            Variable<double> falseDetractor = Variable.Beta(2, 5).Named("falseDetractor");

            //// the Gaussian mixture part of the model
            Range k = new Range(2);
            VariableArray<Vector> means = Variable.Array<Vector>(k).Named("means");

            means[0] = Variable.VectorGaussianFromMeanAndPrecision(Vector.FromArray(-50.0, -5.0, -50, -5), PositiveDefiniteMatrix.IdentityScaledBy(4, 0.1));
            means[1] = Variable.VectorGaussianFromMeanAndPrecision(Vector.FromArray(-100.0, -10.0, -100, -10), PositiveDefiniteMatrix.IdentityScaledBy(4, 0.1));
            VariableArray<PositiveDefiniteMatrix> precisions = Variable.Array<PositiveDefiniteMatrix>(k).Named("precisions");
            precisions[k] = Variable.WishartFromShapeAndScale(10.0, PositiveDefiniteMatrix.IdentityScaledBy(4, 1)).ForEach(k);

            //VariableArray<double> means = Variable.Array<double>(k).Named("means");
            //means[k] = Variable.GaussianFromMeanAndPrecision(0, 0.0001).ForEach(k);
            ////Variable.ConstrainEqual(means[0] > means[1], Variable.Bernoulli(0.9));

            ////Bernoulli[] zinit = new Bernoulli[numSites];
            ////for (int i = 0; i < zinit.Length; i++)
            ////    zinit[i] = Bernoulli.PointMass(Rand.Double() >= 0.5);
            ////site.InitialiseTo(Distribution<bool>.Array(zinit));

            ////Gaussian[] inity = new Gaussian[k.SizeAsInt];
            //////for (int i = 0; i < inity.Length; i++)
            ////inity[0] = Gaussian.FromMeanAndPrecision(10, 0.0001);
            ////inity[1] = Gaussian.FromMeanAndPrecision(-100, 0.0001);
            ////VariableArray<Gaussian> initVar = Variable.Observed(inity, k);
            ////means[k].InitialiseTo(initVar[k]);

            //VariableArray<double> precisions = Variable.Array<double>(k).Named("precisions");
            //precisions[k] = Variable.GammaFromShapeAndRate(1, 1).ForEach(k);

            //VariableArray<double> kpis = Variable.Array<double>(rangeSites).Named("kpis");
            VariableArray<Vector> kpis = Variable.Array<Vector>(rangeSites).Named("kpis").Attrib(new DoNotInfer());

            VariableArray<bool> isMissingVar = Variable.Observed(isMissing, rangeSites);
            VariableArray<bool> hasLabelVar = Variable.Observed(hasLabel, rangeSites);
            //VariableArray<bool> isMissingDetractorVar = Variable.Observed(isMissingDetractor, rangeCustomers);

            //Variable<double> thresholdMean = Variable.GaussianFromMeanAndPrecision(7, 1).Named("thresholdMean");
            //Variable<double> threshold = Variable.GaussianFromMeanAndPrecision(thresholdMean, 1).Named("threshold");

            //VariableArray<double> npsScore = Variable.Array<double>(rangeCustomers).Named("npsScore");
            //npsScore[rangeCustomers] = Variable.GaussianFromMeanAndPrecision(0, 1).ForEach(rangeCustomers);
            //VariableArray<bool> isDetractorTemp = Variable.Array<bool>(rangeCustomers).Named("isDetractorTemp");
            Variable<double> weights = Variable.Beta(1, 160000).Named("weights");

            Variable.ConstrainTrue(Variable.GetItem(means[1], 0) < Variable.GetItem(means[0], 0));
            Variable.ConstrainTrue(Variable.GetItem(means[1], 1) < Variable.GetItem(means[0], 1));
            Variable.ConstrainTrue(Variable.GetItem(means[1], 2) < Variable.GetItem(means[0], 2));
            Variable.ConstrainTrue(Variable.GetItem(means[1], 3) < Variable.GetItem(means[0], 3));

            //Variable.ConstrainEqual(means[0] > means[1], Variable.Bernoulli(0.9));

            // create the Markov chain
            using (var block = Variable.ForEach(rangeSites))
            {
                var t = block.Index;
                var tIsGr = (t > (numUniqueSites - 1));

                using (Variable.If(isMissingVar[t]))
                {
                    site[t] = Variable.Bernoulli(0.5);
                }

                using (Variable.If(hasLabelVar[rangeSites]))
                {
                    Variable.ConstrainEqual(siteLabel[rangeSites], site[rangeSites]);
                }

                //site[t] = Variable.Bernoulli(weights);

                // the first site states
                //using (Variable.IfNot(tIsGr))
                //{
                //    site[t] = Variable.Bernoulli(weights);
                //}

                //// the following site states
                //using (Variable.If(tIsGr))
                //{
                //    var prevT = (t - numUniqueSites);

                //    using (Variable.If(site[prevT]))
                //    {
                //        site[t] = Variable.Bernoulli(0.001);
                //    }
                //    using (Variable.IfNot(site[prevT]))
                //    {
                //        site[t] = Variable.Bernoulli(0.30);
                //    }

                //    Variable.ConstrainEqual(site[t], Variable.Bernoulli(weights));
                //}

                using (Variable.IfNot(isMissingVar[t]))
                {
                    site[t] = Variable.Bernoulli(weights);

                    using (Variable.If(site[t]))
                    {
                        kpis[t] = Variable.VectorGaussianFromMeanAndPrecision(means[1], precisions[1]);
                    }
                    using (Variable.IfNot(site[t]))
                    {
                        kpis[t] = Variable.VectorGaussianFromMeanAndPrecision(means[0], precisions[0]);
                    }
                }
            }


            //// create the Markov chain
            //using (var block = Variable.ForEach(rangeSites))
            //{
            //    site[rangeSites] = Variable.Bernoulli(weights);

            //    //using (Variable.If(hasLabelVar[rangeSites]))
            //    //{
            //    //    Variable.ConstrainEqual(siteLabel[rangeSites], site[rangeSites]);
            //    //}

            //    using (Variable.IfNot(isMissingVar[rangeSites]))
            //    {
            //        // add noise factor for isDetractor observations
            //        using (Variable.If(site[rangeSites]))
            //        {
            //            //site[rangeSites].SetTo(Variable.Bernoulli(trueSiteBad[rangeSites]));
            //            kpis[rangeSites] = Variable.VectorGaussianFromMeanAndPrecision(means[1], precisions[1]);
            //        }
            //        using (Variable.IfNot(site[rangeSites]))
            //        {
            //            //site[rangeSites].SetTo(Variable.Bernoulli(falseSiteBad[rangeSites]));
            //            kpis[rangeSites] = Variable.VectorGaussianFromMeanAndPrecision(means[0], precisions[0]);
            //        }
            //    }
            //    //using (Variable.If(isMissingVar[rangeSites]))
            //    //{
            //    //    // add noise factor for isDetractor observations
            //    //    using (Variable.If(z[rangeSites]))
            //    //    {
            //    //        site[rangeSites].SetTo(Variable.Bernoulli(trueSiteBad[rangeSites]));
            //    //    }
            //    //    using (Variable.IfNot(z[rangeSites]))
            //    //    {
            //    //        site[rangeSites].SetTo(Variable.Bernoulli(falseSiteBad[rangeSites]));
            //    //    }
            //    //}
            //}

            using (Variable.ForEach(rangeCustomers))
            {
                //isDetractorTemp[rangeCustomers] = (npsScore[rangeCustomers] < threshold);

                var relevantSites = Variable.Subarray(site, sitesTouched[rangeCustomers]).Named("relevantSites");

                //create the allTrue factor
                //hadBadSiteInt[rangeCustomers] = Variable.AllTrue(relevantSites).Named("allTrue");

                //create the AnyTrue factor
                var notRelevantSites = Variable.Array<bool>(customersSites);
                notRelevantSites[customersSites] = !relevantSites[customersSites];
                hadBadSiteInt[rangeCustomers] = !Variable.AllTrue(notRelevantSites).Named("anyTrue");

                // add noise factor for isDetractor observations
                using (Variable.If(hadBadSiteInt[rangeCustomers]))
                {
                    // TODO: we will need to think carefully here.
                    //       TP: you had a bad network experience, and is a detractor. 
                    //isDetractor[rangeCustomers].SetTo(Variable.Bernoulli(0.999));
                    isDetractor[rangeCustomers].SetTo(Variable.Bernoulli(trueDetractor));
                    //isDetractor[rangeCustomers].SetTo(Variable.Bernoulli(trueDetractor[rangeCustomers]));
                }
                using (Variable.IfNot(hadBadSiteInt[rangeCustomers]))
                {
                    // TODO: we will need to think carefully here.
                    //       FP: you had a good network experience, but is a detractor.
                    //isDetractor[rangeCustomers].SetTo(Variable.Bernoulli(0.999));
                    isDetractor[rangeCustomers].SetTo(Variable.Bernoulli(falseDetractor));
                    //isDetractor[rangeCustomers].SetTo(Variable.Bernoulli(falseDetractor[rangeCustomers]));
                }

                //using (Variable.IfNot(isMissingDetractorVar[rangeCustomers]))
                //{
                //    Variable.ConstrainEqual(isDetractorLabel[rangeCustomers], isDetractor[rangeCustomers]);
                //}

                //Variable.ConstrainEqual(isDetractor[rangeCustomers], isDetractorTemp[rangeCustomers]);
            }

            /********* observations *********/
            isDetractor.ObservedValue = isDetractorAnswers;
            //npsScore.ObservedValue = npsAnswers;
            numberOfsitesForEachCustomer.ObservedValue = numSitesForEachCustomer;
            sitesTouched.ObservedValue = sitesIndicesForEachCustomer;
            kpis.ObservedValue = netKPIsVector;
            //siteLabel.ObservedValue = label;
            /*******************************/

            /********** inference **********/
            var InferenceEngine = new InferenceEngine(new ExpectationPropagation());
            //var InferenceEngine = new InferenceEngine(new VariationalMessagePassing());
            InferenceEngine.NumberOfIterations = 30;
            //InferenceEngine.ShowFactorGraph = true;

            //Bernoulli[] isDetractorPosteriors = InferenceEngine.Infer<Bernoulli[]>(isDetractor);
            Bernoulli[] sitesPosteriors = InferenceEngine.Infer<Bernoulli[]>(site);
            Bernoulli[] hadBadPosteriors = InferenceEngine.Infer<Bernoulli[]>(hadBadSiteInt);
            Beta trueDetractorPosteriors = InferenceEngine.Infer<Beta>(trueDetractor);
            Beta falseDetractorPosteriors = InferenceEngine.Infer<Beta>(falseDetractor);

            //Beta[] falseSiteBadPosteriors = InferenceEngine.Infer<Beta[]>(falseSiteBad);
            //Beta[] trueSiteBadPosteriors = InferenceEngine.Infer<Beta[]>(trueSiteBad);

            Beta weightsPosteriors = InferenceEngine.Infer<Beta>(weights);
            VectorGaussian[] meansPosteriors = InferenceEngine.Infer<VectorGaussian[]>(means);
            Wishart[] precisionsPosteriors = InferenceEngine.Infer<Wishart[]>(precisions);
            //Gaussian postThresholdMean = InferenceEngine.Infer<Gaussian>(thresholdMean);
            ///*******************************/

            for (int i = 0; i < 2; i++)
            {
                Console.WriteLine("Posterior Gaussian: {0}", meansPosteriors[i]);
                Console.WriteLine("Posterior Gamma: {0}", precisionsPosteriors[i].GetMean().Inverse());
            }

            //Console.WriteLine(postThresholdMean);
            //Console.WriteLine(postThresholdMean);

            Console.WriteLine("True detractor: {0}", trueDetractorPosteriors);
            Console.WriteLine("False detractor: {0}", falseDetractorPosteriors);
            Console.WriteLine("Weights: {0}", weightsPosteriors);

            /*********** outputs ***********/
            var storeWeights = new StringBuilder();
            var line = string.Format("{0}", weightsPosteriors.GetMean());
            storeWeights.AppendLine(line);

            var storeSites = new StringBuilder();

            for (int i = 0; i < numSites; i++)
            {
                line = string.Format("{0}", sitesPosteriors[i].GetProbTrue());
                storeSites.AppendLine(line);
            }

            var storeCustomer = new StringBuilder();

            for (int i = 0; i < numCustomers; i++)
            {
                line = string.Format("{0}", hadBadPosteriors[i].GetProbTrue());
                storeCustomer.AppendLine(line);
            }

            var storeGMM = new StringBuilder();

            for (int i = 0; i < 2; i++)
            {
                var meanVec = meansPosteriors[i].GetMean();
                var varMat = precisionsPosteriors[i].GetMean().Inverse();
                //var newLine = string.Format("{0},{1},{2},{3},{4},{5}", meanVec[0], meanVec[1], varMat[0], varMat[1], varMat[2], varMat[3]);
                var newLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19}", meanVec[0], meanVec[1], meanVec[2], meanVec[3], varMat[0], varMat[1], varMat[2], varMat[3], varMat[4], varMat[5], varMat[6], varMat[7], varMat[8], varMat[9], varMat[10], varMat[11], varMat[12], varMat[13], varMat[14], varMat[15]);
                storeGMM.AppendLine(newLine);
            }

            File.WriteAllText("sites-results.csv", storeSites.ToString());
            File.WriteAllText("customer-results.csv", storeCustomer.ToString());
            File.WriteAllText("gmm-results.csv", storeGMM.ToString());
            File.WriteAllText("weights-results.csv", storeWeights.ToString());
        }
    }
}
