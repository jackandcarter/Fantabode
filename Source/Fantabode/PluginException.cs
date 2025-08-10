using System;

namespace Fantabode
{
  public class PluginException : Exception
  {
    public PluginException() { }
    public PluginException(string message) : base($"Fantabode Exception: {message}") { }
  }
}
