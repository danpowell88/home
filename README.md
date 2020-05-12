# My House

## TODO

### General
* ~~Implement outside rooms (LaundryOutside, Patio, GymOutside, GarageSide, FrontEntry)~~
* ~~Dont trigger lights to turn on when presence ended, just start timer~~
* ~~Ensure light triggers all obey "motion enabled" and rename to be more generic~~
* ~~Security notifications when doors/windows/covers opened and sleeping or not home~~
* Implement concept of supplemental lighting for feature lights
* Ignore lights on based on lux
* Set google home volume based on time of day / bed presence

### Presence
* Turn off ~~lights~~ and devices when left home
* Implement concept of "always on" switches so they dont get switched off by automations

### Cooling / Heating
* Enable/Disable fan / air conditioning when room has presence for x and temperature is x

### Notifications
* TTS and app notification depending on presence / occupancy
* ~~Low batteries~~
* Device dropped from zigbee/wifi for x period
* New device added to zigbee (need to add mqtt connection to ND - ON HOLD) 
* Traffic to work notification
* Weather notifications

### Room Specific

#### Laundry
* Migrate washing machine automation

#### Master Bedroom
* ~~Get bed occupancy working~~
* Sleeping bayesian sensors
* Wake / Sleep automations
* General motion based on wake/sleep
* ~~Master switches to turn off devices/lights~~
* Add double switch for fan
* Bed accent lighting
* Turn on / of lights depending on if bed state is off and has been off for > 10 minutes

#### Ensuite Shower
* Turn on fan when humidity is high after shower
* ~~Turn off fan after on for x time~~

#### Ensuite Toilet
* ~~Turn off fan when on for x time~~

#### Bathroom
* ~~Turn off fan when on for x time~~

#### Pantry
* Fridge left open notification

#### Kitchen
* ~~Migrate dishwasher automation~~
* Trigger vacuum in kitchen after dishwasher on (?)

#### Media
* ~~Figure out why lights turn off when chair has presence / power detected~~
* Light off when movie playing
* Lights back on when paused / finished

##### Study
* Implement IoTLink for pc idle status override https://gitlab.com/iotlink/iotlink/-/wikis/Addons/Windows-Monitor
