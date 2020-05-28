---
uid: samples
---

# Samples

## Silverback.Examples Solution

A solution containing a few examples can be found under the `samples/Examples` directory of the [Silverback repository](https://github.com/BEagle1984/silverback/tree/develop/samples/Examples). The same solution, targeting .NET Core 2.2 can be found in `samples/Examples-2.2`.

It includes a sample consumer (`Silverback.Examples.ConsumerA`) and a sample producer (`Silverback.Examples.Main`) implementing several common use cases. Just run both console applications to see the samples in action.

<figure>
	<a href="~/images/samples.png"><img src="~/images/samples.png"></a>
</figure>

## Environment

To execute the samples you need a running Apache Kafka (obviously) and SQL Server.

Use the [docker compose file](https://github.com/BEagle1984/silverback/tree/develop/docker-compose.yaml) in the root of the repository to startup the environment.