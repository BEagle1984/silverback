* [33md39eca6[m[33m ([m[1;36mHEAD -> [m[1;32mLoggingAndErrorHandling[m[33m, [m[1;32mLoggingAndErrorHandling2[m[33m)[m Partial refactoring
* [33m033c534[m[33m ([m[1;31morigin/LoggingAndErrorHandling[m[33m)[m ErrorPolicy static class and some details
* [33me0699fd[m SkipMessageErrorPolicy
* [33ma2c306e[m MoveMessageErrorPolicy
* [33mcc789ff[m Wired error policy in SimpleInboundAdapter + unit tests
* [33med05e85[m First ErrorPolicy implementation (with RetryErrorPolicy)
[31m|[m * [33m88d92c1[m[33m ([m[1;31morigin/feature/kafka_integration[m[33m)[m Producer, configure await false.
[31m|[m * [33m892e6b4[m handle producer istance
[31m|[m * [33m04c58f3[m async producer example
[31m|[m * [33m236ee87[m async subscriber
[31m|[m * [33ma971ba6[m code cleanup
[31m|[m * [33mcb858d7[m kafka endpoint moved in Silverback.Messaging namespace
[31m|[m * [33m9a03263[m async producer
[31m|[m * [33m296b1a0[m non-blocking consumer
[31m|[m * [33ma5bcb42[m[33m ([m[1;32mfeature/kafka_integration[m[33m)[m Review
[31m|[m * [33mb000afc[m Samples with new version of libraries
[31m|[m * [33m41fb93c[m Silverback.Integration to latest version
[31m|[m * [33m925a23f[m Kafka broker disconnect method for producers
[31m|[m * [33m954588a[m disposing consumer
[31m|[m * [33m4fe78b9[m Producer Disconnect method
[31m|[m * [33m0239317[m latest version of silverback.core
[31m|[m *   [33mf695a1b[m solve conflicts
[31m|[m [33m|[m[34m\[m  
[31m|[m [33m|[m * [33mc19b283[m kafka integration
[31m|[m [33m|[m *   [33m9480a0b[m Merge branch 'master' of https://github.com/BEagle1984/silverback
[31m|[m [33m|[m [35m|[m[31m\[m  
[31m|[m [33m|[m[31m_[m[35m|[m[31m/[m  
[31m|[m[31m/[m[33m|[m [35m|[m   
[31m|[m * [35m|[m [33m3827104[m Removed reference to KafkaInbound/Outbound adapters
[31m|[m * [35m|[m [33mf2f6fe0[m code cleaning
[31m|[m * [35m|[m [33mcecf643[m Added common.logging and cleaning code
[31m|[m * [35m|[m [33m22291dd[m Added commit features in the consumer
[31m|[m * [35m|[m [33m631798f[m Removed KafkaInbound/Outbound adapters
[31m|[m * [35m|[m [33m0f690ec[m Consumer parameters in kafka endpoint
[31m|[m * [35m|[m [33m55f7089[m Improved Equality Check.
[31m|[m * [35m|[m [33m332e1cf[m commit offset if autocommit disabled
[31m|[m * [35m|[m [33m8a9a52b[m IEquatable kafka endpoint
[31m|[m * [35m|[m [33m1eb9b8d[m basic implementation
[31m|[m [35m|[m[35m/[m  
[31m|[m * [33m4e2e12f[m updated reference to latest version of silverback core
[31m|[m [36m|[m * [33m7629bdc[m[33m ([m[1;31morigin/SilverbackShop[m[33m, [m[1;32mSilverbackShop[m[33m)[m JsonExceptionsMiddleware
[31m|[m [36m|[m * [33m25baf99[m Making it work in docker
[31m|[m [36m|[m * [33mf5a2410[m Unique constraint on Product.SKU
[31m|[m [36m|[m * [33me989277[m Workaround for FileSystemWatcher not properly working with docker mounted volume
[31m|[m [36m|[m * [33mbe9800c[m Docker support
[31m|[m [36m|[m * [33mf265d26[m Sqllite in Basket.Service and migrate on startup
[31m|[m [36m|[m * [33m1d389d2[m Use IEnumerable instead of array in GenericFactory
[31m|[m [36m|[m[31m/[m  
[31m|[m[31m/[m[36m|[m   
* [36m|[m   [33md66fd7f[m[33m ([m[1;31morigin/master[m[33m, [m[1;31morigin/HEAD[m[33m, [m[1;32mmaster[m[33m)[m Merge pull request #7 from BEagle1984/AutoSubscribe
[1;32m|[m[1;33m\[m [36m\[m  
[1;32m|[m * [36m|[m [33mb049521[m Upgraded silverback libs and c# version
[1;32m|[m * [36m|[m [33ma469bee[m Upgrade core and integration to latest C# version
[1;32m|[m * [36m|[m [33m844636d[m Multiple subscribers resoltion and AutoSubscribe config method
[1;32m|[m[1;32m/[m [36m/[m  
* [36m|[m   [33m3463a3a[m Merge pull request #6 from BEagle1984/SilverbackShop
[36m|[m[1;35m\[m [36m\[m  
[36m|[m [1;35m|[m[36m/[m  
[36m|[m[36m/[m[1;35m|[m   
[36m|[m * [33m4092271[m Introduce Sqllite in Catalog.Service
[36m|[m * [33m2090d3a[m Fix merge issues
[36m|[m * [33mcd6cd91[m Catalog events consumed by Basket.Service
[36m|[m *   [33m2e1f858[m Merge branch 'AsyncMessageHandlers' into SilverbackShop
[36m|[m [1;36m|[m[31m\[m  
[36m|[m [1;36m|[m * [33ma6e9634[m Update SilverbackShop
[36m|[m [1;36m|[m * [33m3419a0a[m Updated Silverback.Integration.FileSystem
[36m|[m [1;36m|[m * [33m46c29f9[m Update Silverback.Integration
[36m|[m [1;36m|[m * [33m8790c04[m Test SilverbackDbContext
[36m|[m [1;36m|[m * [33m9e7b016[m Test BusConfig and publishers
[36m|[m [1;36m|[m * [33m1a74062[m Test subscribers
[36m|[m [1;36m|[m * [33m8652e84[m Bus tests
[36m|[m [1;36m|[m * [33mdfbb222[m Tremendous refactoring: bus rewritten without rx, unified subscribers/handlers and added async subscribers.
[36m|[m * [31m|[m [33m8e74fe9[m Details on repository
[36m|[m [31m|[m[31m/[m  
[36m|[m * [33m10df652[m Catalog.Service
[36m|[m * [33md8a8f63[m Clean up
[36m|[m * [33ma07576b[m Cleanup
[36m|[m * [33mda8d9fa[m Implementing CatalogService
[36m|[m * [33m4728be4[m Extra test
[36m|[m * [33m4f33332[m IAggregateRepository in Silverback.Core
[36m|[m * [33mfe8ede0[m Adjust namespaces
[36m|[m * [33mfd975f6[m Updated libs, changed namespaces and created Catalog.Service
* [31m|[m   [33maecc189[m Merge pull request #5 from BEagle1984/BrokerConnectionManagement
[32m|[m[31m\[m [31m\[m  
[32m|[m [31m|[m[31m/[m  
[32m|[m * [33m7799674[m Updated Silverback.Testing
* [33m|[m   [33m4cd6044[m Merge pull request #4 from BEagle1984/BrokerConnectionManagement
[34m|[m[33m\[m [33m\[m  
[34m|[m [33m|[m[33m/[m  
[34m|[m * [33mb1df8cc[m Add some tests / do some clean up
[34m|[m * [33mdc12cf5[m Fix and test connect/disconnect
[34m|[m * [33m1b9c0f3[m Connect/Disconnect refactoring
[34m|[m * [33m63f4efd[m Refactoring broker management: brokers are now bound to the bus and the Connect method allow to warm them up estabilishing the connection etc.
[34m|[m * [33m749691e[m IBus.Items
[34m|[m * [33m721417b[m Refactoring IConsumer
[34m|[m * [33mc3d0017[m Make Core internals visible to Integration project
[34m|[m[34m/[m  
[34m|[m *   [33m7e86f24[m[33m ([m[1;35mrefs/stash[m[33m)[m On BrokerConnectionManagement: TryConnectionManagement
[34m|[m [34m|[m[1;31m\[m  
[34m|[m[34m/[m [1;31m/[m  
[34m|[m * [33mda54046[m index on BrokerConnectionManagement: 4d9bec9 Merge pull request #2 from BEagle1984/IConfigurator
[34m|[m[34m/[m  
*   [33m4d9bec9[m Merge pull request #2 from BEagle1984/IConfigurator
[1;32m|[m[1;33m\[m  
[1;32m|[m * [33m9fbb918[m IConfigurator
[1;32m|[m * [33mc3378e9[m Update Silverback.Integration to use new simplified subscribers.
[1;32m|[m * [33mf5b786f[m Fix Core.EfCore dependency
[1;32m|[m * [33mb42a7e7[m Incremented package versions
[1;32m|[m * [33mc1e77d4[m Resolved warnings
[1;32m|[m * [33m99b6f77[m Resolved warnings
[1;32m|[m[1;32m/[m  
* [33m5ebd746[m MultiMessageHandler sample
*   [33m658c7d5[m Merge pull request #1 from BEagle1984/feature/MultiMessageHandler
[1;34m|[m[1;35m\[m  
[1;34m|[m * [33m28f4194[m Version increment
[1;34m|[m * [33m5948d8f[m MultiMessageHandler
[1;34m|[m * [33m0e25082[m Renaming test types
[1;34m|[m * [33m6e54809[m Simplify Subscriber and DefaultSubscriber (filtering capabilities moved to IMessageHandler implementation)
[1;34m|[m * [33m68fa124[m Allow filter in message handler directly (useful for MultiMessageHandler)
[1;34m|[m[1;34m/[m  
* [33md9e94ae[m Fix .gitignore
* [33mfef568e[m Fixed nuget package dir name
* [33mcdbb90c[m Import from old repositories
* [33medc3fb1[m Initial commit
