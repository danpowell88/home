# My House

## TODO

### General
* ~~Implement outside rooms (LaundryOutside, Patio, GymOutside, GarageSide, FrontEntry)~~
* ~~Dont trigger lights to turn on when presence ended, just start timer~~
* ~~Ensure light triggers all obey "motion enabled" and rename to be more generic~~
* ~~Security notifications when doors/windows/covers opened and sleeping or not home~~
* ~~Implement concept of supplemental lighting for feature lights~~
* Ignore lights on based on lux
* ~~Set google home volume based on time of day / bed presence~~
* Bin day notification

### Presence
* Turn off ~~lights~~ and devices when left home
* Implement concept of "always on" switches so they dont get switched off by automations
* ~~Disable motion when no one home~~
* Turn off lights when someone left and other person is in bed
* Dog BT trackers

### Security
* open/close notification when sleeping

### Cooling / Heating
* Enable/Disable fan / air conditioning when room has presence for x and temperature is x

### Notifications
* ~~TTS and app notification depending on presence / occupancy~~
* ~~Low batteries~~ -TO BE TESTED
* ~~Device dropped from zigbee/wifi for x period~~ - TO BE TESTED
* Traffic to work notification
* Weather notifications
* Greeting notification with events since left
* Janet inspired conversation -https://github.com/Lentron/Janet---Home-Assistant/blob/master/packages/janet.yaml
* Calendar events upcoming, and time to leave

### Vacuum
* Stop vacuum when tv show started
* ~~Stop vacuum when someone returns home~~

### Room Specific

#### Laundry
* ~~Migrate washing machine automation~~ - TO BE TESTED

#### Master Bedroom
* ~~Get bed occupancy working~~
* Sleeping bayesian sensors
* Wake / Sleep automations
* General motion based on wake/sleep
* ~~Master switches to turn off devices/lights~~
* Add double switch for fan
* ~~Bed accent lighting~~
* ~~Turn on / of lights depending on if bed state is off and has been off for > 10 minutes~~
* ~~Master off switches action depends on number of people in bed and people home~~
* Cycle secondary light colours
* Turn off secondary lights when everyone is in bed
** When someone gets in bed turn off
** When bed count changes but someone still in bed turn on
* ~~Turn down speaker volume when someone gone to bed~~

#### Ensuite Shower
* Turn on fan when humidity is high after shower
* ~~Turn off fan after on for x time~~  - TO BE TESTED

#### Ensuite Toilet
* ~~Turn off fan when on for x time~~ - TO BE TESTED

#### Bathroom
* ~~Turn off fan when on for x time~~ - TO BE TESTED

#### Pantry
* ~~Fridge left open notification~~ -  TO BE TESTED

#### Kitchen
* ~~Migrate dishwasher automation~~
* Trigger vacuum in kitchen after dishwasher on, if tv not on and no one in bed

#### Media
* ~~Figure out why lights turn off when chair has presence / power detected~~
* ~~Light off when movie playing~~ - TO BE TESTED
* ~~Lights back on when paused / finished~~ - TO BE TESTED
* TV / Amp power monitoring
* IR to turn on tv and amp
* Google voice to turn on tv etc

#### Study
* Implement IoTLink for pc idle status override https://gitlab.com/iotlink/iotlink/-/wikis/Addons/Windows-Monitor
* Verify monitors turn off when pc off

#### Garage
* ~~Door left open notification~~
* ~~Disable motion when everyone in bed~~