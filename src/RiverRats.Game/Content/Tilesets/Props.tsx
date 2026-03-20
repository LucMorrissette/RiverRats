<?xml version="1.0" encoding="UTF-8"?>
<tileset version="1.10" tiledversion="1.12.0" name="Props" tilewidth="96" tileheight="64" tilecount="7" columns="0">
  <tile id="0">
    <properties>
      <property name="propType" value="boulder"/>
    </properties>
    <image source="../Sprites/boulder.png" width="32" height="32"/>
  </tile>
  <tile id="1">
    <properties>
      <property name="propType" value="dock"/>
    </properties>
    <image source="../Sprites/wooden-dock.png" width="64" height="64"/>
  </tile>
  <tile id="2">
    <properties>
      <property name="propType" value="sunken-log"/>
      <property name="isUnderwater" type="bool" value="true"/>
    </properties>
    <image source="../Sprites/sunken-log.png" width="96" height="32"/>
  </tile>
  <tile id="3">
    <properties>
      <property name="propType" value="dock-leg-left"/>
      <property name="isUnderwater" type="bool" value="true"/>
      <property name="reachesSurface" type="bool" value="true"/>
    </properties>
    <image source="wooden-dock-leg-left.png" width="32" height="32"/>
  </tile>
  <tile id="4">
    <properties>
      <property name="propType" value="firepit"/>
    </properties>
    <image source="../Sprites/basic-firepit.png" width="32" height="24"/>
  </tile>
  <tile id="5">
    <properties>
      <property name="propType" value="small-fire"/>
    </properties>
    <image source="small-fire-preview.png" width="16" height="16"/>
  </tile>
  <tile id="6">
    <properties>
      <property name="propType" value="sunken-chest"/>
      <property name="isUnderwater" type="bool" value="true"/>
    </properties>
    <image source="../Sprites/sunken-chest.png" width="32" height="17"/>
  </tile>
</tileset>
