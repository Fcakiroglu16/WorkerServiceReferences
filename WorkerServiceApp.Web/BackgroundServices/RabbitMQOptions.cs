using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkerServiceApp.Web.BackgroundServices
{
    public class RabbitMQOptions
    {
        public string ConnectionString { get; set; }

        public string QueueName { get; set; }
    }
}