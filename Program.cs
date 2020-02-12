using System;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;

namespace New_folder
{

    public class PizzaOrders {
        public string[] toppings {get; set;}
    }
    public class PizzaMetric {
        public string[] toppings {get; set;}
        public string displayval {get; set;}
        public int hash {get; set;}
        public int count {get; set; }
        public int rank {get; set; }
    }

    public static class HttpDataRequest  {
       public static async Task<PizzaOrders []> RequestPizzaOrders() 
        {
            try {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("http://files.olo.com");
                HttpResponseMessage response = await client.GetAsync("pizzas.json");
                if (response.IsSuccessStatusCode)
                {
                    var dto = response.Content.ReadAsStringAsync().Result;
                    var settings = new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            MissingMemberHandling = MissingMemberHandling.Ignore
                        };
                    return JsonConvert.DeserializeObject<PizzaOrders []>(dto, settings);
                }
                else
                {
                    Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception Thrown {0}", ex.Message);
            }

            PizzaOrders[] p = new PizzaOrders[0];
            return p;
        }
    }


    class Program
    {
        static async Task Main(string[] args)
        {
            var pOrders = await HttpDataRequest.RequestPizzaOrders();
            if (pOrders==null || (pOrders!=null && pOrders.Length==0)) return;

            //Use a hash incase the toppings are not in the same order. Assume lowercase for now
            var pMetrics = (from po in pOrders
                    select new PizzaMetric{
                        displayval=string.Join(", ", po.toppings),
                        toppings=po.toppings,
                        hash = po.toppings.Sum(topval => (System.Text.ASCIIEncoding.ASCII.GetBytes(topval).Select(x => (int)x).ToArray()).Sum())
                    });

            var OrdersByCount = (from po in pMetrics
                    group po by po.hash into g
                    select new PizzaMetric {
                        displayval=g.First().displayval,
                        toppings=g.FirstOrDefault().toppings,
                        hash = g.FirstOrDefault().hash,
                        count=g.Count()
                    }).OrderByDescending(p => p.count).ToList();

            using (var sw = new StreamWriter("README.md"))
            {
                sw.WriteLine(@"# Pizza");
                sw.WriteLine();
                sw.WriteLine(@"Produced with C# Core 3.0");
                sw.WriteLine();
                sw.WriteLine(@"##Top 20 Combinations");
                int i=1;
                OrdersByCount.ForEach(po => {po.rank=i++; 
                    if (i<20)
                    sw.WriteLine($"- {po.displayval} ");
                });


                sw.WriteLine();
                sw.WriteLine($"##Order and Rank");
                sw.WriteLine($"##Rank    Count   Combination");
                OrdersByCount.OrderBy(p => p.displayval).ToList().ForEach(po => {
                    sw.WriteLine($"- {po.rank}   {po.count}   {po.displayval}");
                });
            }
        }
    }
}
