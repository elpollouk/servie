This directory should contain local user specific configuration files. These files are not
intended to be shipped as part of your packaged environment and are meant as a way for a user
to override particular configuration settings if needed.

The files in here don't need to be complete configuration files as they are applied to the config
files that are provided with the environment when Servie is launched.

An environment.xml file in this directory can be used to override settings in the
\packages\servie\environment.xml. An example of this would be to secify where the local JDK is
installed by setting the JAVA_HOME environment variable. Although, for a real stand alone
environment, you would ship the JDK aswell in a directory such as \packages\jdk and you would
set JAVA_HOME to ..\..\packages\jdk in \packages\servie\environment.xml.

It is also possible to override individual settings in a server's servie.xml file if you need
to make changes to a server's configuration. For example, it is possible to override settings in
\servers\apache-tomcat-6.0.29\servie.xml by placing a file caled apache-tomcat-6.0.29.xml in this
directory.

An example apache-tomcat-6.0.29.xml file would be:
  <xml>
    <start>
      <exec>
        <args>jpda start</args>
      </exec>
    </start>
  </xml>

Although this file doesn't specify a complete configuration, it is perfectly valid. In this
example, it simply changes the starting command line arguments so that Tomcat is launched
with the debugger enabled. This might be something that a developer might want to enable on their
local machine, but have it turned off by default in the servie.xml so that it isn't enabled for
non-developer users.
