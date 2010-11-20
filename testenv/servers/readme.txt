Place servers in sub directories of this directory.

When Servie lauches, it scans sub directories in this directory for servie.xml. These files are the
configuration files used by Servie to start and stop the server. The general format of a servie.xml
file is:

<xml>
<!--
  Enable this tag if you want Servie to completely ignore this service.
  Optional.
-->
  <ignore>true/false</ignore>
<!--
  Set this property if you want to use a different display name for this service.
  Optional.
-->
  <name>Dummie</name>
<!--
  Enable this tag if you want Servie to start the service automatically when it launches.
  Optional.
-->
  <autostart>true/false</autostart>

<!--
  Specifies the start command to use
  Required.
-->
  <start>
    <exec>
    <!--
      This only needs to be set if the working dir is different to the server base dir.
	  If no working directory is specified, then the server's base director is used.
	  Optional.
    -->
      <workingdir>\some\other\path</workingdir>
	<!--
	  The executable to start the server.
	  Required.
	-->
      <executable>server.exe</executable>
    <!--
      Command line arguments for the executables.
	  Optional.
    -->
      <args>parm1 parm2 "Parameter Three"</args>

    <!-- 
      Environment variables required for this service.
	  Optional.
    -->
      <env>
        <VAR1>Value 1</VAR1>
		<VAR2>Value 2</VAR2>
      </env>
    </exec>
    <!--
      Wait delay after starting service. If the service isn't still running after this time, it is
	  considered to have failed.
    -->
    <wait>1000</wait>
  </start>

<!--
  Specifies the stop command to use.
  Either the signal or the kill method must be specified, but it is not possible to specify both.
  Required.
-->
  <stop>
  <!--
    Sends a console signal to the service to stop it.
    Valid signals are:
      CTRL_C
      CTRL_BREAK
      CTRL_CLOSE
      CTRL_LOGOFF
      CTRL_SHUTDOWN
	Can't be used with the kill stop command.
  -->
    <signal>CTRL_C</signal>
  <!--
    Kills the process to end the service
	Can't be used with the signal stop command.
  -->
    <kill/>
  <!--
    Shutdown time out after which the user will the prompted if they want to continue waiting.
	If the user doesn't wait, then the process will be killed.
	Optional.
  -->
    <timeout>10000</timeout>
  </stop>
</xml>