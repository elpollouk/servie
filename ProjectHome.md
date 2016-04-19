Servie is a simple utility that allows you to run a number of command line started servers (e.g. Apache Tomcat or Cassandra) within a single application window. Each server can be configured with its own execution environment allowing you to run them within their own stand alone directory without needing to install them as a Windows service.

It is primarily aimed at developers who want to be able to deliver a complete environment that may be made up of multiple servers to someone else for review so that it can be launched by simply double clicking the Servie executable.

Developers may also find it handy while developing locally as it prevents your desktop and task bar becoming cluttered with multiple console windows. All your server TTY is gathered into one place that can easily be minimised to the system tray when you don't need to see it.

![http://wiki.servie.googlecode.com/hg/images/serviewindow.png](http://wiki.servie.googlecode.com/hg/images/serviewindow.png)

## What Servie Is Not ##

Servie is not a production server environment. It is purely intended for use in a development, testing and demo capacity.

Servie is also not a replacement for [XAMPP](http://www.apachefriends.org/en/xampp.html). If you're already set up using XAMPP, then you will probably see very little benefit from Servie unless you are using additional servers that aren't integrated into XAMPP.