using System;

namespace KvStore.Router.Models;

public sealed record ForwardedKeyValueResult(
    KeyValueRecord Record,
    string NodeId,
    TimeSpan ExecutionTime);


