﻿using MessageSilo.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageSilo.Shared.Models
{
    public class LastMessage
    {
        public Message Input { get; set; }

        public Message? Output { get; set; }

        public string? Error { get; set; }

        public LastMessage(Message input)
        {
            Input = input.GetCopy();
        }

        public LastMessage()
        {
        }

        public void SetOutput(Message? output, string? error)
        {
            Output = output?.GetCopy();
            Error = error;
        }

        public override string ToString()
        {
            return YamlConverter.Serialize(this);
        }
    }
}
