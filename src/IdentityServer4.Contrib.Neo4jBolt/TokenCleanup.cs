using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.Neo4jBolt
{
    public class TokenCleanupOptions
    {
        public int Interval { get; set; } = 60;
    }

    class TokenCleanup
    {
        readonly IServiceProvider _serviceProvider;
        readonly TimeSpan _interval;
        CancellationTokenSource _source;

        public TokenCleanup(IServiceProvider serviceProvider, TokenCleanupOptions options)
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.Interval < 1) throw new ArgumentException("interval must be more than 1 second");

            _serviceProvider = serviceProvider;
            _interval = TimeSpan.FromSeconds(options.Interval);
        }

        public void Start()
        {
            if (_source != null) throw new InvalidOperationException("Already started. Call Stop first.");

            _source = new CancellationTokenSource();
            Task.Factory.StartNew(() => Start(_source.Token));
        }

        public void Stop()
        {
            if (_source == null) throw new InvalidOperationException("Not started. Call Start first.");

            _source.Cancel();
            _source = null;
        }

        async Task Start(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await Task.Delay(_interval, cancellationToken);
                }
                catch
                {
                    break;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                ClearTokens();
            }
        }

        void ClearTokens()
        {
            try
            {
                var nowString = DateTime.UtcNow.ToString("o");

                var neo4jDriver = Neo4jProvider.Instance.Driver;
                Configuration cfg = Configuration.Instance;

                using (var session = neo4jDriver.Session(AccessMode.Write))
                using (var tx = session.BeginTransaction())
                {
                    tx.Run($@"MATCH (grant:{cfg.PersistedGrantLabel})
                              WHERE grant.Expired < {{now}}
                              DELETE grant", new Dictionary<string, object> { { "now", nowString} }).Consume();
                    
                }
            }
            catch (Exception ex)
            {
                //skip
            }
        }
    }
}
