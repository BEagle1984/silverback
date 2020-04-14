---
title: Exactly-once processing with offset storage
category: Release
tags: Features Release
---

The `DbOffsetStoredInboundConnector` has finally been released. It offers a more optimized way to guarantee that each message is consumed exactly once, storing just the offset of the latest consumed message(s).

Have a look at the [Inbound Connector documentation]({{ site.baseurl }}/docs/configuration/inbound) for more information.