// developed by Rui Lopes (ruilopes.com). licensed under MIT.

using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MailBounceDetector
{
    // The headers are described at https://tools.ietf.org/html/rfc3464
    // The status codes are described at https://tools.ietf.org/html/rfc3463
    public sealed class BounceDetectResult
    {
        private readonly BounceStatus _primaryStatus;
        private readonly BounceStatus _secundaryStatus;
        private readonly BounceStatus _combinedStatus;
        private readonly string _reportingMta;
        private readonly string _remoteMta;
        private readonly string _finalRecipient;
        private readonly string[] _diagnosticCodes;
        private readonly string _action;
        private readonly MimeEntity _deliveryNotificationPart;
        private readonly MessageDeliveryStatus _deliveryStatus;
        private readonly MimeEntity _undeliveredMessagePart;
        private readonly string _undeliveredMessageId;

        private static readonly IDictionary<string, Regex> Matchers = new Dictionary<string, Regex>(StringComparer.InvariantCultureIgnoreCase)
        {
            ["Final-Recipient"] = r(@"(.+);\s*(.*)"),
            ["Action"] = r(@"(failed|delayed|delivered|relayed|expanded)"),
            ["Status"] = r(@"([0-9]+)\.([0-9]+)\.([0-9]+)"),
            ["Reporting-MTA"] = r(@"(.+);\s*(.*)"),
            ["Remote-MTA"] = r(@"(.+);\s*(.*)"),
            ["Diagnostic-Code"] = r(@"(.+);\s*([0-9\-\.]+)?\s*(.*)"),
        };

        private static Regex r(string regex)
        {
            return new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        }

        private static readonly IDictionary<int, string> PrimaryStatusCodes = new Dictionary<int, string>
            {
                { 1, "Unknown Status Code 1" },
                { 2, "Success" },
                { 3, "Temporary Failure" },
                { 4, "Persistent Transient Failure" },
                { 5, "Permanent Failure" },
            };

        private static readonly IDictionary<int, string> SecundaryStatusCodes = new Dictionary<int, string>
            {
                { 0, "Other or Undefined Status" },
                { 1, "Addressing Status" },
                { 2, "Mailbox Status" },
                { 3, "Mail System Status" },
                { 4, "Network and Routing Status" },
                { 5, "Mail Delivery Protocol Status" },
                { 6, "Message Content or Media Status" },
                { 7, "Security or Policy Status" },
            };

        private static readonly IDictionary<int, string> CombinedStatusCodes = new Dictionary<int, string>
            {
                { 00, "Not Applicable" },
                { 10, "Other address status" },
                { 11, "Bad destination mailbox address" },
                { 12, "Bad destination system address" },
                { 13, "Bad destination mailbox address syntax" },
                { 14, "Destination mailbox address ambiguous" },
                { 15, "Destination mailbox address valid" },
                { 16, "Mailbox has moved" },
                { 17, "Bad sender\'s mailbox address syntax" },
                { 18, "Bad sender\'s system address" },

                { 20, "Other or undefined mailbox status" },
                { 21, "Mailbox disabled, not accepting messages" },
                { 22, "Mailbox full" },
                { 23, "Message length exceeds administrative limit." },
                { 24, "Mailing list expansion problem" },

                { 30, "Other or undefined mail system status" },
                { 31, "Mail system full" },
                { 32, "System not accepting network messages" },
                { 33, "System not capable of selected features" },
                { 34, "Message too big for system" },

                { 40, "Other or undefined network or routing status" },
                { 41, "No answer from host" },
                { 42, "Bad connection" },
                { 43, "Routing server failure" },
                { 44, "Unable to route" },
                { 45, "Network congestion" },
                { 46, "Routing loop detected" },
                { 47, "Delivery time expired" },

                { 50, "Other or undefined protocol status" },
                { 51, "Invalid command" },
                { 52, "Syntax error" },
                { 53, "Too many recipients" },
                { 54, "Invalid command arguments" },
                { 55, "Wrong protocol version" },

                { 60, "Other or undefined media error" },
                { 61, "Media not supported" },
                { 62, "Conversion required and prohibited" },
                { 63, "Conversion required but not supported" },
                { 64, "Conversion with loss performed" },
                { 65, "Conversion failed" },

                { 70, "Other or undefined security status" },
                { 71, "Delivery not authorized, message refused" },
                { 72, "Mailing list expansion prohibited" },
                { 73, "Security conversion required but not possible" },
                { 74, "Security features not supported" },
                { 75, "Cryptographic failure" },
                { 76, "Cryptographic algorithm not supported" },
                { 77, "Message integrity failure" },
            };

        private BounceStatus ParseBounceStatus(string statusCode, IDictionary<int, string> statusCodes)
        {
            var value = int.Parse(statusCode);
            return new BounceStatus(value, statusCodes[value]);
        }

        public BounceDetectResult(
            MimeMessage message,
            MimeEntity deliveryNotification,
            MessageDeliveryStatus deliveryStatus,
            MimeEntity undeliveredMessagePart
        )
        {
            var headers = new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase);

            if (deliveryStatus != null)
            {
                foreach (var header in deliveryStatus.StatusGroups.SelectMany(g => g).Where(h => Matchers.ContainsKey(h.Field)))
                {
                    var m = Matchers[header.Field].Match(header.Value);

                    if (!m.Success)
                        continue;

                    var matched = m.Groups.Cast<Group>().Skip(1).Select(c => c.Value).ToArray();

                    if (matched.Length > 0)
                    {
                        headers[header.Field] = matched;
                    }
                }
            }

            if (headers.ContainsKey("Status"))
            {
                var status = headers["Status"];
                _primaryStatus = ParseBounceStatus(status[0], PrimaryStatusCodes);
                _secundaryStatus = ParseBounceStatus(status[1], SecundaryStatusCodes);
                _combinedStatus = ParseBounceStatus(string.Join("", status.Skip(1)), CombinedStatusCodes);
            }

            if (headers.ContainsKey("Remote-Mta"))
            {
                _remoteMta = headers["Remote-Mta"][1];
            }

            if (headers.ContainsKey("Reporting-Mta"))
            {
                _reportingMta = headers["Reporting-Mta"][1];
            }

            if (headers.ContainsKey("Final-Recipient"))
            {
                _finalRecipient = headers["Final-Recipient"][1];
            }

            if (headers.ContainsKey("Diagnostic-Code"))
            {
                _diagnosticCodes = headers["Diagnostic-Code"].Skip(1).ToArray();
            }

            if (headers.ContainsKey("Action"))
            {
                _action = headers["Action"][0];
            }

            _deliveryNotificationPart = deliveryNotification;
            _deliveryStatus = deliveryStatus;
            _undeliveredMessagePart = undeliveredMessagePart;

            // get the original message id.
            if (_undeliveredMessagePart != null)
            {
                var undeliveredMessage = ((MessagePart)_undeliveredMessagePart).Message;
                _undeliveredMessageId = undeliveredMessage.Headers["Message-Id"];
            }
            // try harder. Exchange has In-Reply-To right on the root message. its the undelivered message id.
            else
            {
                _undeliveredMessageId = message.Headers["In-Reply-To"];
            }
        }

        public bool IsBounce { get { return _deliveryStatus != null; } }
        public bool IsHard { get { return IsBounce && _primaryStatus != null && _primaryStatus.Code > 4; } }
        public bool IsSoft { get { return IsBounce && _primaryStatus != null && _primaryStatus.Code <= 4; } }
        public BounceStatus PrimaryStatus { get { return _primaryStatus; } }
        public BounceStatus SecundaryStatus { get { return _secundaryStatus; } }
        public BounceStatus CombinedStatus { get { return _combinedStatus; } }
        public string ReportingMta { get { return _reportingMta; } }
        public string RemoteMta { get { return _remoteMta; } }
        public string FinalRecipient { get { return _finalRecipient; } }
        public string[] DiagnosticCodes { get { return _diagnosticCodes; } }
        public string Action { get { return _action; } }
        // normally this is-a TextPart.
        public MimeEntity DeliveryNotificationPart { get { return _deliveryNotificationPart; } }
        public MessageDeliveryStatus DeliveryStatus { get { return _deliveryStatus; } }
        // normally this is-a MessagePart.
        public MimeEntity UndeliveredMessagePart { get { return _undeliveredMessagePart; } }
        public string UndeliveredMessageId { get { return _undeliveredMessageId; } }

        public override string ToString()
        {
            return _primaryStatus != null
                ? $"{_primaryStatus.Message}, {_secundaryStatus.Message}, {_combinedStatus.Message}"
                : "Not a bounce message.";
        }
    }
}
