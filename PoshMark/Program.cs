using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Poshmark
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Initializing!");
            Server server = new Server();
            //Initializing the Server types and its cost
            server.init();

            //Use Case 1 : Get Cost for minimum 115 CPUs for 24 Hours.
            server.getCost(24, 115, 0);
            //Use Case 2: Get Cost for many possible CPUs for 29$ for 8 Hours
            server.getCost(8, 0, 29);
            //Use Case 3: Get Cost for minimum 214 CPUs and less than 95$ for 7hours
            server.getCost(7, 214, 95);


        }
    }

    class Server
    {
        private Dictionary<string, int> CPUCount = new Dictionary<string, int>()
        {
            { "large",1 },
            { "xlarge",2 },
            {"2xlarge", 4},
            {"4xlarge", 8},
            {"8xlarge", 16},
            {"10xlarge", 32}

        };

        private Dictionary<string, Dictionary<string, double>> DataCenters = new Dictionary<string, Dictionary<string, double>>()
        {
            { "us-east",
                new Dictionary<string, double>(){
                    { "large",0.12 },
                    { "xlarge",0.23 },
                    {"2xlarge", 0.45},
                    {"4xlarge", 0.774},
                    {"8xlarge", 1.4},
                    {"10xlarge", 2.28}
                }
            },
            { "us-west",
                new Dictionary<string, double>(){
                    { "large",0.14 },
                    {"2xlarge", 0.413},
                    {"4xlarge", 0.89},
                    {"8xlarge", 1.3},
                    {"10xlarge", 2.97}
                }
            },
            { "asia",
                new Dictionary<string, double>(){
                    { "large",0.11 },
                    { "xlarge", 0.2},
                    {"4xlarge", 0.67},
                    {"8xlarge", 1.18},
                }
            }
        };

        public void init()
        {
            //sort CPUCount dictionary by its Count
            CPUCount = sortCountByDesc(CPUCount);

            //sort Datacenter Server types by its cost per CPU to consider the best Server type first 
            foreach (KeyValuePair<string, Dictionary<string, double>> dataCenter in DataCenters.ToList())
            {
                DataCenters[dataCenter.Key] = sortServerByCostPerCPU(DataCenters[dataCenter.Key]);
            }
        }
        public List<Dictionary<string, object>> getCost(int hours, int cpus, float price)
        {
            Console.WriteLine("Input");
            Console.WriteLine("Hours " + hours + " cpus " + cpus + " price " + price);
            List<Dictionary<string, object>> ResultList = new List<Dictionary<string, object>>();

            if (price != 0)
            {
                foreach (KeyValuePair<string, Dictionary<string, double>> dataCenter in DataCenters)
                {
                    Dictionary<string, object> result = new Dictionary<string, object>();
                    Dictionary<string, int> utilisationServers = new Dictionary<string, int>();
                    double remainingCost = price;
                    int totalUtilizedCPUs = 0;
                    foreach (KeyValuePair<string, double> dataCenterServers in dataCenter.Value)
                    {
                        double serverCost = dataCenterServers.Value;
                        string serverName = dataCenterServers.Key;
                        int currentServerCPUCount = CPUCount[dataCenterServers.Key];
                        double currentServerTotalCost = serverCost * hours;
                        if (remainingCost > currentServerTotalCost && (cpus == 0 || totalUtilizedCPUs < cpus))
                        {
                            totalUtilizedCPUs = totalUtilizedCPUs + currentServerCPUCount;
                            utilisationServers.Add(dataCenterServers.Key, (int)Math.Floor(price / currentServerTotalCost));
                            remainingCost = remainingCost - currentServerTotalCost;
                        }
                    }
                    if (utilisationServers.Count > 0)
                    {
                        result.Add("region", dataCenter.Key);
                        result.Add("total_cost", "$" + Math.Round((price - remainingCost), 2));
                        result.Add("servers", utilisationServers);
                        ResultList.Add(result);
                    }
                }
            }
            else
            {
                foreach (KeyValuePair<string, Dictionary<string, double>> dataCenter in DataCenters)
                {
                    Dictionary<string, object> result = new Dictionary<string, object>();
                    Dictionary<string, int> utilisationServers = new Dictionary<string, int>();
                    int totalUtilizedCPUs = 0;
                    double totalServerCost = 0;
                    int remainingRequiredCPUs = cpus;
                    foreach (KeyValuePair<string, double> dataCenterServers in dataCenter.Value)
                    {
                        double serverCost = dataCenterServers.Value;
                        string serverName = dataCenterServers.Key;
                        int serverCUPCount = CPUCount[dataCenterServers.Key];

                        if (remainingRequiredCPUs != 0 && serverCUPCount <= remainingRequiredCPUs)
                        {
                            int currentUtilizingServers = remainingRequiredCPUs / serverCUPCount;
                            int currentUtilizingCPUs = currentUtilizingServers * CPUCount[dataCenterServers.Key];
                            double currentServercost = serverCost * hours * currentUtilizingServers;
                            totalServerCost = totalServerCost + currentServercost;
                            totalUtilizedCPUs = totalUtilizedCPUs + currentUtilizingCPUs;
                            remainingRequiredCPUs = remainingRequiredCPUs - currentUtilizingCPUs;
                            utilisationServers.Add(dataCenterServers.Key, currentUtilizingServers);
                        }
                    }
                    if (totalUtilizedCPUs >= cpus)
                    {
                        result.Add("region", dataCenter.Key);
                        result.Add("total_cost", "$" + Math.Round(totalServerCost, 2));
                        result.Add("servers", utilisationServers);
                        ResultList.Add(result);
                    }

                }
            }
            if (ResultList.Count > 0)
            {
                ResultList = orderByTotalCost(ResultList);
            }
            Console.WriteLine("Output");
            Console.WriteLine(JsonConvert.SerializeObject(ResultList, Formatting.Indented));
            return ResultList;
        }

        private Dictionary<string, int> sortCountByDesc(Dictionary<string, int> dic)
        {
            return dic.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        }

        private Dictionary<string, double> sortServerByCostPerCPU(Dictionary<string, double> dic)
        {
            return dic.OrderBy(x => x.Value / CPUCount[x.Key]).ToDictionary(x => x.Key, x => x.Value);
        }
        private List<Dictionary<string, object>> orderByTotalCost(List<Dictionary<string, object>> dic)
        {
            return dic.OrderBy(x => x["total_cost"]).ToList();
        }


    }
}
