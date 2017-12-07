<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
  <xsl:output method="xml" indent="yes"/>

  <xsl:param name="packagesConfigPath"/>

  <xsl:template match="@* | node()">
    <xsl:copy>
      <xsl:apply-templates select="@* | node()"/>
    </xsl:copy>
  </xsl:template>

  <!-- Insert dependencies -->
  <xsl:template match="//metadata">
    <xsl:copy>
      <xsl:apply-templates select="@* | node()"/>
      <xsl:element name="dependencies">
        <xsl:for-each select="document($packagesConfigPath)//package[not(@developmentDependency)]">
          <xsl:element name="dependency">
            <xsl:attribute name="id">
              <xsl:value-of select="@id"/>
            </xsl:attribute>
            <xsl:attribute name="version">
              <xsl:value-of select="@version"/>
            </xsl:attribute>
          </xsl:element>
        </xsl:for-each>
      </xsl:element>
    </xsl:copy>
  </xsl:template>


</xsl:stylesheet>