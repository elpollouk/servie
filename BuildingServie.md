

# Introduction #

Hopefully, building Servie from source should be fairly straight forward. However, there are a couple of local changes you may need to make in order to get it up and running in the debugger. This simply involves setting the correct working directory in Visual Studio and potentially installing a JDK and setting up a local JAVA\_HOME environment variable.

## Required software ##

  * [Visual C# 2010 Express](http://www.microsoft.com/express/Downloads/#2010-Visual-CS)
  * [Java SE JDK](http://www.oracle.com/technetwork/java/javase/downloads/index.html)

Servie has been developed purely using Visual C# 2010 Express and so you shouldn't need a paid-for version of Visual Studio to build it. It should build in the full version of Visual Studio 2010, but I haven't tested it myself. I can't guarantee that it will build in any previous version of Visual Studio (e.g. 2008) as I think the directory enumeration code I'm using is .Net 4 only unfortunately.

If you wish to run using the supplied test environment, then you will also need a Java JDK installed. This is because [Apache Tomcat](http://tomcat.apache.org/) requires a JDK to run in order to compile JSP pages. Normally, you would want to supply any dependencies such as the JDK along with the environment and reference that version, but I needed to keep the size of the repository down. Also, as a developer, you may already have a JDK installed and probably don't want to have to download another one.

# Setting the working directory in Visual Studio #

When you open the solution for the first time, you will need to set the correct working directory otherwise Servie will complain that it can't find a servers directory when you try to run it in the debugger. Right click on the Servie project (not the solution) in the solution explorer and select "Properties". In the new window that opens, go to the "Debug" section. You should now be able to set the working directory. Browse to the testenv directory that should be in the same location as the Servie.sln file.

# Testenv #

The testenv directory contains a test environment for Servie. Most of the servers are simply various configs using the Dummie test application, although there is a distribution of [Apache Tomcat](http://tomcat.apache.org/) to prove that a real server can be managed by Servie. Servie has no official links with nor is endorsed by the Apache Tomcat project.

Servers are configured through sub directories found in the servie\testenv\servers directory. More details of these configurations can be found [here](http://servie.googlecode.com/hg/testenv/servers/readme.txt).

## Changes to Apache Tomcat ##

The only change that I've made to Tomcat is in the catalina.bat file. In the official distribution, the server is launched in a new window using the "start java ..." command. This doesn't work with Servie as it means that the catalina.bat script will exit nearly immediately after being called and Tomcat will be running in a completely independent process. To fix this, I have removed the "start" command so that catalina.bat launches the Java VM directly and waits for it to finish before exiting.

# Configuring JAVA\_HOME #

[Apache Tomcat](http://tomcat.apache.org/) requires a JDK to run which is specified with the JAVA\_HOME environment variable. If you have already configured this to point to a JDK at a system level, then you won't need to do anything else. Tomcat will probably launch without any problems. However, if you don't have this variable set (the JDK doesn't set it as apart of the installation) or you have it pointing at a runtime rather than a JDK, then you will need to create a local config for your test environment.

First, you will need to locate the JDK you wish to use. On my PC, this is in C:\Program Files\Java\jdk1.6.0\_22. Then, in the servie\testenv\packages\servie\localconf directory, create a new file called environment.xml. This file will contain custom environment variables that are specific to your local set up. Edit the environment.xml file so that it looks something like the following example:

```
<xml>
  <!-- Obviously, this should point to the location of a JDK on your PC! -->
  <JAVA_HOME>C:\Program Files\Java\jdk1.6.0_22</JAVA_HOME>
</xml>
```

More details on the files in the localconf directory can be found [here](http://servie.googlecode.com/hg/testenv/packages/servie/localconf/readme.txt).