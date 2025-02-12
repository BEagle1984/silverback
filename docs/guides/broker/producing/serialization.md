---
uid: serialization
---

# Serialization

The message payload needs to be serialized into a byte array before it can be sent to the broker. By default, Silverback used `System.Text.Json` to serialize the message payload. This can be customized tweaking the serializer settings, using another one of the built-in serializers or implementing a custom serializer.

_...coming soon..._
