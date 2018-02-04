var fireworksMenu = null;

var isPlacing = false;
var placingModel = -1;
var placingAnim = "";
var placingTime = 0;

API.onServerEventTrigger.connect(function(eventName, args) {
    if (eventName == "Firework_AnimReporter")
    {
        isPlacing = true;
        placingModel = args[0];
        placingAnim = args[1];
        placingTime = API.getGlobalTime();
    }
});

API.onUpdate.connect(function() {
    if (isPlacing)
    {
        if (API.getAnimCurrentTime(API.getLocalPlayer(), "anim@mp_fireworks", placingAnim) >= 0.6)
        {
            isPlacing = false;

            var tempProp = API.createObject(placingModel, API.getOffsetInWorldCoords(API.getLocalPlayer(), new Vector3(0.0, 0.45, 0.0)), new Vector3());
            API.callNative("PLACE_OBJECT_ON_GROUND_PROPERLY", tempProp);
            API.triggerServerEvent("Firework_CompletePlace", API.getEntityPosition(tempProp));
            API.deleteEntity(tempProp);
        }

        if (API.getGlobalTime() - placingTime >= 5000)
        {
            isPlacing = false;
            API.triggerServerEvent("Firework_Cancel");
        }
    }
});

API.onKeyUp.connect(function(e, key) {
    if (key.KeyCode == Keys.F10)
    {
        if (fireworksMenu == null) {
            fireworksMenu = API.createMenu(API.getGameText("PIM_TFIREW"), API.getGameText("PIM_UFIREW"), 0, 0, 6);
            API.setMenuBannerRectangle(fireworksMenu, 255, 224, 50, 50);

            // Type
            let fireworksList = new List(String);
            for (let i = 0; i < 4; i++) fireworksList.Add(API.getGameText("PM_FWTY" + i));

            let fireworksItem = API.createListItem(API.getGameText("PIM_TFWTY"), API.getGameText("PIM_HFWTY0"), fireworksList, 0);
            fireworksMenu.AddItem(fireworksItem);

            // Color
            let colorsList = new List(String);
            colorsList.Add(API.getGameText("HUD_COLOUR_RED"));
            colorsList.Add(API.getGameText("HUD_COLOUR_WHITE"));
            colorsList.Add(API.getGameText("HUD_COLOUR_BLUE"));

            let colorsItem = API.createListItem(API.getGameText("PIM_TFWCO"), "", colorsList, 0);
            fireworksMenu.AddItem(colorsItem);

            // Timer
            let timerList = new List(String);
            for (let i = 0; i <= 60; i++) timerList.Add(i.toString());

            let delayItem = API.createListItem(API.getGameText("PIM_TFWTI"), API.getGameText("PIM_HFWTI"), timerList, 0);
            fireworksMenu.AddItem(delayItem);

            // Place
            let placeItem = API.createMenuItem(API.getGameText("PIM_TFWPLC"), API.getGameText("PIM_HFWPLC"));
            fireworksMenu.AddItem(placeItem);

            // Play
            let playItem = API.createMenuItem(API.getGameText("PIM_TFWPLY"), API.getGameText("PIM_HFWPLY"));
            fireworksMenu.AddItem(playItem);

            // Events
            fireworksItem.OnListChanged.connect(function(item, newIndex) {
                fireworksItem.Description = API.getGameText("PIM_HFWTY" + newIndex);
            });

            placeItem.Activated.connect(function(menu, item) {
                if (!isPlacing) API.triggerServerEvent("Firework_BeginPlace", fireworksItem.Index, colorsItem.Index, delayItem.Index);
            });

            playItem.Activated.connect(function(menu, item) {
                API.triggerServerEvent("Firework_Launch");
                fireworksMenu.Visible = false;
            });

            fireworksMenu.Visible = true;
        } else {
            fireworksMenu.Visible = !fireworksMenu.Visible;
        }
    }
});

API.onEntityStreamIn.connect(function(entity, entityType) {
    if (entityType == 9)
    {
        if (API.hasEntitySyncedData(entity, "FireworkColor"))
        {
            var data = JSON.parse(API.getEntitySyncedData(entity, "FireworkColor"));
            API.callNative("SET_PARTICLE_FX_LOOPED_COLOUR", entity, API.f(data.red / 255), API.f(data.green / 255), API.f(data.blue / 255), 0);
        }
    }
});