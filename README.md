# MailBounceDetector

[![Build status](https://github.com/rgl/MailBounceDetector/actions/workflows/build.yml/badge.svg)](https://github.com/rgl/MailBounceDetector/actions/workflows/build.yml)

this library detects whether a message is a [bounce message](https://en.wikipedia.org/wiki/Bounce_message).

this detects [standard](#standard-bounce-messages) and [qmail](#qmail-bounce-messages) bounce messages.

see the [unit tests](MailBounceDetector.Tests/BounceDetectorTests.cs) for an example on how that information is exposed by the library.

## Standard bounce messages

a bounce message is one that has (or is) a `multipart/report; report-type=delivery-status` MIME part. its comprised of two or three sub-parts:

1. the human readable description of the bounce. normally this is a `text/plain` or `text/html` part.
2. the machine readable description of the bounce. this is a `message/delivery-status` part.
3. the original message that bounced. this part is optional, and might not have the complete message. its useful to known some of the original message headers such as the `Message-Id`. this is normally a `message/rfc822` part.

the most important part is the `message/delivery-status` part; it looks something like:

```plain
Content-Type: message/delivery-status

Reporting-MTA: dns; PTPEDGE02.test.local

Final-recipient: RFC822;
    email_that_does_not_exists_this_is_just_a_test@gmail.com
Action: failed
Status: 5.1.1
Remote-MTA: dns; mx.google.com
X-Supplementary-Info: <mx.google.com #5.1.1 smtp;550-5.1.1 The email account
    that you tried to reach does not exist.Please try 550-5.1.1 double-checking
the recipient's email address for typos or 550-5.1.1 unnecessary spaces.
    Learn more at 550 5.1.1  https://support.google.com/mail/answer/6596
    om11si19081667wic.29 - gsmtp>
```

### Standard bounce messages references

* [RFC6522 about the `multipart/report` part: The Multipart/Report Media Type for the Reporting of Mail System Administrative Messages](https://tools.ietf.org/html/rfc6522)
* [RFC3464 about the `message/delivery-status` part: An Extensible Message Format for Delivery Status Notifications](https://tools.ietf.org/html/rfc3464)
* [RFC3463 about the `message/delivery-status` part status codes: Enhanced Mail System Status Codes](https://tools.ietf.org/html/rfc3463)

## qmail bounce messages

a bounce message is one that is a `text/plain` (which can also be inside a `multipart/alternative`) MIME part, e.g.:

 ```plain
Date: 17 Mar 1996 03:54:40 -0000
From: MAILER-DAEMON@silverton.berkeley.edu
To: djb@silverton.berkeley.edu
Subject: failure notice

Hi. This is the qmail-send program at silverton.berkeley.edu.
I'm afraid I wasn't able to deliver your message to the
following addresses. This is a permanent error; I've given up.
Sorry it didn't work out.

<god@heaven.af.mil>:
Sorry, I couldn't find any host by that name.

--- Below this line is a copy of the message.

Return-Path: <djb@silverton.berkeley.edu>
Received: (qmail 317 invoked by uid 7); 17 Mar 1996 03:54:38 -0000
Date: 17 Mar 1996 03:54:38 -0000
Message-ID: <19960317035438.316.qmail@silverton.berkeley.edu>
From: djb@silverton.berkeley.edu (D. J. Bernstein)
To: god@heaven.af.mil
Subject: are you there?

Just checking.
```

### qmail bounce messages references

* [The qmail-send Bounce Message Format (QSBMF)](http://cr.yp.to/proto/qsbmf.txt)
