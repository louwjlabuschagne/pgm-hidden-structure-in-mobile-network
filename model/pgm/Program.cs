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


            int numberOfIterations = 30;
            int NULL_KPI_VALUE = 100000;//default value in KPI data when no data is available

            int numDays = -1;
            int numUniqueSites = -1;
            
            
            int numCustomers = -1;
            string dataDir = "";
            
            string datasetFilename  = "";
            string responsesFilename  = "";
            string kpisFilename  = "";
            string labelsFilename = "";

            int NR_DIMS = 2;
            int NR_CLASSES = 2;//Promoter or Detractor

            float mu_0_dim_0 = 0.0f; //mean prior for the site performance for the promoter cluster dimension 0
            float mu_0_dim_1 = 0.0f; //mean prior for the site performance for the promoter cluster dimension 1
            float prec_0 = 0.1f; //precision prior for the site performance for the promoter cluster dimension 1


            // for detractors
            float mu_1_dim_0 = 0.0f; //mean prior for the site performance for the detractor cluster dimension 0
            float mu_1_dim_1 = 0.0f; //mean prior for the site performance for the detractor cluster dimension 1
            float prec_1 = 0.1f; //precision prior for the site performance for the detractor cluster dimension 1

            float wishart_shape = 0.1f; //wishart shape
            float wishart_scale = 1.0f; //wishart scale

            float DEFAULT_SITE_LABEL_PRIOR = 0.5f;//the default p value for the bernouli if there is no label for a site
            float SITE_PEFORMANCE_PRIOR = 0.5f;//

            float TRUE_COUNT_PRIOR = 5; //count of promoters that are really promoters, and count of true detractors that are really detractors
            float FALSE_COUNT_PRIOR = 2; //count of promoters that are not really promoters, and count of detractors that are not really detractors

            // here we set both TRUE and FALSE to 1 as we are unsure at the start
            float TRUE_IS_BAD_SITE_PRIOR = 1; //count of sites that are really bad sites
            float FALSE_IS_BAD_SITE_PRIOR = 1; //count of sites that are really good sites

            


            if (args.Length == 0) {
                // Display message to user to provide parameters.
                System.Console.WriteLine ("Please enter parameter values.");
                System.Console.WriteLine ("dotnet run dataDirectory numDays numSites numCustomers");
                System.Console.WriteLine ("It's assumed that dataDirectory will contain: interactions.csv responses.csv dataset-kpis.csv labels.csv");
                System.Console.WriteLine ("dotnet run .\\data\\survey-responses.csv 1 3 2");

                Console.Read ();
            } else {
                // Loop through array to list args parameters.
                for (int i = 0; i < args.Length; i++) {
                    switch (i) {
                        case 0:
                            dataDir = args[i];
                            break;
                        case 1:
                            datasetFilename = args[i];
                            break;
                        case 2:
                            responsesFilename = args[i];
                            break;
                        case 3:
                            kpisFilename = args[i];
                            break;
                        case 4:
                            labelsFilename = args[i];
                            break;
                        case 5:
                            numDays = Int16.Parse (args[i]);
                            break;
                        case 6:
                            numUniqueSites = Int16.Parse (args[i]);
                            break;
                    }

                }
                // Keep the console window open after the program has run.
                // Console.Read ();
            }

            int numSites = numDays * numUniqueSites;

            datasetFilename = dataDir + "/input/" + datasetFilename;
            responsesFilename = dataDir + "/input/" + responsesFilename;
            kpisFilename = dataDir + "/input/" + kpisFilename;
            labelsFilename = dataDir + "/input/" + labelsFilename;


            // the data.csv file contains all the customer site connections for all days.
            // string fileName = "19-01-2021-datasets/interactions.csv";
            string[] lines = File.ReadAllLines(datasetFilename);
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

            numCustomers = lines.Length;

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
            // fileName = "19-01-2021-datasets/responses.csv";
            lines = File.ReadAllLines(responsesFilename);
            bool[] isDetractorAnswers = new bool[lines.Length];

            for (int i = 0; i < lines.Length; i++)
            {
                string[] strArray = lines[i].Split('|');
                int[] intArray = Array.ConvertAll(strArray, int.Parse);

                if (intArray[1] == 1)
                {
                    isDetractorAnswers[i] = true;
                }
                else
                {
                    isDetractorAnswers[i] = false;
                }
            }

            // the kpi.csv file contains all the network kpis.
            // fileName = "19-01-2021-datasets/dataset-kpis.csv";
            lines = File.ReadAllLines(kpisFilename);
            double[] netKPIs = new double[numSites];
            bool[] isMissing = new bool[numSites];
            Vector[] netKPIsVector = new Vector[numSites];

            for (int i = 0; i < numSites; i++)
            {
                string[] strArray = lines[i].Split(';');
                double[] doubleArray = Array.ConvertAll(strArray, double.Parse);

                // 2 dimentional vector of kpis
                if (doubleArray[0] == NULL_KPI_VALUE | doubleArray[1] == NULL_KPI_VALUE)
                {
                    isMissing[i] = true;
                }
                else
                {
                    isMissing[i] = false;
                    netKPIsVector[i] = Vector.FromArray(doubleArray);
                }
            }

            // the labels.csv file contains all the semi-supervised labels.
            // fileName = "19-01-2021-datasets/labels.csv";
            lines = File.ReadAllLines(labelsFilename);
            bool[] hasLabel = new bool[numSites];
            bool[] label = new bool[numSites];

            for (int i = 0; i < numSites; i++)
            {
                string[] strArray = lines[i].Split(';');
                double[] doubleArray = Array.ConvertAll(strArray, double.Parse);

                if (doubleArray[0] == NULL_KPI_VALUE)
                {
                    hasLabel[i] = false;
                }
                else
                {
                    hasLabel[i] = true;

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

            Console.WriteLine("Total number of customers (from nps file): {0}", isDetractorAnswers.Length);

            Range rangeSites = new Range(numSites);
            Range rangeCustomers = new Range(numCustomers);

            VariableArray<int> numberOfsitesForEachCustomer = Variable.Array<int>(rangeCustomers).Named("numSitesForCustomers").Attrib(new DoNotInfer());
            Range customersSites = new Range(numberOfsitesForEachCustomer[rangeCustomers]).Named("customersSites");

            VariableArray<VariableArray<int>, int[][]> sitesTouched = Variable.Array(Variable.Array<int>(customersSites), rangeCustomers).Named("sitesTouched").Attrib(new DoNotInfer());

            VariableArray<bool> site = Variable.Array<bool>(rangeSites).Named("sites");

            VariableArray<bool> siteLabel = Variable.Array<bool>(rangeSites).Named("sitesLabel");
            siteLabel[rangeSites] = Variable.Bernoulli(DEFAULT_SITE_LABEL_PRIOR).ForEach(rangeSites);

            VariableArray<bool> isDetractor = Variable.Array<bool>(rangeCustomers).Named("isDetractor");

            VariableArray<bool> hadBadSiteInt = Variable.Array<bool>(rangeCustomers).Named("hadBadSiteInt");

            Variable<double> trueDetractor = Variable.Beta(TRUE_COUNT_PRIOR, FALSE_COUNT_PRIOR).Named("trueDetractor");
            Variable<double> falseDetractor = Variable.Beta(FALSE_COUNT_PRIOR, TRUE_COUNT_PRIOR).Named("falseDetractor");

            //// the Gaussian mixture part of the model
            Range k = new Range(NR_CLASSES);
            VariableArray<Vector> means = Variable.Array<Vector>(k).Named("means");

            means[0] = Variable.VectorGaussianFromMeanAndPrecision(Vector.FromArray(mu_0_dim_0, mu_0_dim_1), PositiveDefiniteMatrix.IdentityScaledBy(NR_DIMS, prec_0));
            means[1] = Variable.VectorGaussianFromMeanAndPrecision(Vector.FromArray(mu_1_dim_0, mu_1_dim_1), PositiveDefiniteMatrix.IdentityScaledBy(NR_DIMS, prec_1));
            VariableArray<PositiveDefiniteMatrix> precisions = Variable.Array<PositiveDefiniteMatrix>(k).Named("precisions");
            precisions[k] = Variable.WishartFromShapeAndScale(wishart_shape, PositiveDefiniteMatrix.IdentityScaledBy(NR_DIMS, wishart_scale)).ForEach(k);

            VariableArray<Vector> kpis = Variable.Array<Vector>(rangeSites).Named("kpis").Attrib(new DoNotInfer());

            VariableArray<bool> isMissingVar = Variable.Observed(isMissing, rangeSites);
            VariableArray<bool> hasLabelVar = Variable.Observed(hasLabel, rangeSites);
            Variable<double> weights = Variable.Beta(TRUE_IS_BAD_SITE_PRIOR, FALSE_IS_BAD_SITE_PRIOR).Named("weights");

            // enforces that the mean of the one cluster is greater than the other
            Variable.ConstrainTrue(Variable.GetItem(means[1], 0) < Variable.GetItem(means[0], 0));
            Variable.ConstrainTrue(Variable.GetItem(means[1], 1) < Variable.GetItem(means[0], 1));

            /**************************************************************************************************************
                                                    Baysian GMM for KPIS
            **************************************************************************************************************/
            using (var block = Variable.ForEach(rangeSites))
            {
                var t = block.Index;
                var tIsGr = (t > (numUniqueSites - 1));

                using (Variable.If(isMissingVar[t]))
                {
                    site[t] = Variable.Bernoulli(SITE_PEFORMANCE_PRIOR);
                }

                using (Variable.If(hasLabelVar[rangeSites]))
                {
                    Variable.ConstrainEqual(siteLabel[rangeSites], site[rangeSites]);
                }

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

            /**************************************************************************************************************
                                             Noisy NPS survey responses (OR logic)
            **************************************************************************************************************/
            using (Variable.ForEach(rangeCustomers))
            {
                var relevantSites = Variable.Subarray(site, sitesTouched[rangeCustomers]).Named("relevantSites");

                //create the AnyTrue factor
                var notRelevantSites = Variable.Array<bool>(customersSites);
                notRelevantSites[customersSites] = !relevantSites[customersSites];
                hadBadSiteInt[rangeCustomers] = !Variable.AllTrue(notRelevantSites).Named("anyTrue");

                // add noise factor for isDetractor observations
                using (Variable.If(hadBadSiteInt[rangeCustomers]))
                {
                    isDetractor[rangeCustomers].SetTo(Variable.Bernoulli(trueDetractor));
                }
                using (Variable.IfNot(hadBadSiteInt[rangeCustomers]))
                {
                    isDetractor[rangeCustomers].SetTo(Variable.Bernoulli(falseDetractor));
                }
            }

            /********* observations *********/
            isDetractor.ObservedValue = isDetractorAnswers;
            numberOfsitesForEachCustomer.ObservedValue = numSitesForEachCustomer;
            sitesTouched.ObservedValue = sitesIndicesForEachCustomer;
            kpis.ObservedValue = netKPIsVector;
            siteLabel.ObservedValue = label;
            /*******************************/

            /********** inference **********/
            var InferenceEngine = new InferenceEngine(new ExpectationPropagation());
            InferenceEngine.NumberOfIterations = numberOfIterations;
            //InferenceEngine.ShowFactorGraph = true;

            Bernoulli[] sitesPosteriors = InferenceEngine.Infer<Bernoulli[]>(site);
            Bernoulli[] hadBadPosteriors = InferenceEngine.Infer<Bernoulli[]>(hadBadSiteInt);
            Beta trueDetractorPosteriors = InferenceEngine.Infer<Beta>(trueDetractor);
            Beta falseDetractorPosteriors = InferenceEngine.Infer<Beta>(falseDetractor);

            Beta weightsPosteriors = InferenceEngine.Infer<Beta>(weights);
            VectorGaussian[] meansPosteriors = InferenceEngine.Infer<VectorGaussian[]>(means);
            Wishart[] precisionsPosteriors = InferenceEngine.Infer<Wishart[]>(precisions);
            ///*******************************/

            for (int i = 0; i < NR_CLASSES; i++)
            {
                Console.WriteLine("Posterior Gaussian: {0}", meansPosteriors[i]);
                Console.WriteLine("Posterior Gamma: {0}", precisionsPosteriors[i].GetMean().Inverse());
            }

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
                line = string.Format("{0};{1}", i, hadBadPosteriors[i].GetProbTrue());
                storeCustomer.AppendLine(line);
            }

            var storeGMM = new StringBuilder();

            for (int i = 0; i < NR_CLASSES; i++)
            {
                var meanVec = meansPosteriors[i].GetMean();
                var varMat = precisionsPosteriors[i].GetMean().Inverse();
                var newLine = string.Format("{0};{1};{2};{3};{4};{5}", meanVec[0], meanVec[1], varMat[0], varMat[1], varMat[2], varMat[3]);
                storeGMM.AppendLine(newLine);
            }


            File.WriteAllText(dataDir+"/output/sites-results.csv", storeSites.ToString());
            File.WriteAllText(dataDir+"/output/customer-results.csv", storeCustomer.ToString());
            File.WriteAllText(dataDir+"/output/gmm-results.csv", storeGMM.ToString());
            File.WriteAllText(dataDir+"/output/weights-results.csv", storeWeights.ToString());
        }
    }
}
