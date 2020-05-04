# My House


## TODO

### General
Implement outside rooms (LaundryOutside, Patio, GymOutside, GarageSide, FrontEntry)
Security notifications when doors/windows/covers opened and sleeping or not home
Implement concept of supplemental lighting for feature lights
Implement motion hours of operation override (maybe)
Ignore lights on based on lux

### Presence
Turn off lights and devices when left home
Implement concept of "always on" switches so they dont get switched off by automations

### Cooling / Heating
Enable/Disable fan / air conditioning when room has presence for x and temperature is x

### Notifications
TTS and app notification depending on presence / occupancy
Low batteries
Device dropped from zigbee/wifi for x period
New device added to zigbee 
Traffic to work notification
Weather notifications

### Room Specific

#### Laundry
Migrate washing machine automation

#### Master Bedroom
Get bed occupancy working
Sleeping bayesian sensors
Wake / Sleep automations
General motion based on wake/sleep
Master switches to turn off devices/lights
Add double switch for fan
Bed accent lighting

#### Ensuite Shower
Turn on fan when humidity is high after shower
Turn off fan after on for x time

#### Ensuite Toilet
Turn off fan when on for x time

#### Bathroom
Turn off fan when on for x time

#### Pantry
Fridge left open notification

#### Kitchen
Migrate dishwasher automation
Trigger vacuum in kitchen after dishwasher on (?)

#### Media
Light off when movie playing
Lights back on when paused / finished