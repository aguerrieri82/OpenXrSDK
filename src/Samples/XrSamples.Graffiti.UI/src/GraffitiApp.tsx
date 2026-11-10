import { router, App, localTable } from "@eusoft/webapp-framework";
import { MainPage } from "./pages/MainPage";
import { appBridge } from "./services/Bridge";

export class GraffitiApp extends App {

    protected override async onStarted() {

        appBridge.start();
        localTable.language = "IT";
        Object.defineProperty(router, "currentLocation", {
            get: () => new URL(location.href.replace("ui://", "http://")).pathname
        });
        router.addPage(MainPage);
        router.startAsync();
    }
}
