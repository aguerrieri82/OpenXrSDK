import { BaseBridge } from "@eusoft/xrsdk-ui";
import { Guid, IGraffitiInfo } from "../entities/Types";

class Bridge extends BaseBridge {

    async listGraffitiAsync() {
        return await this.callAsync<IGraffitiInfo[]>("ListGraffiti", {});
    }

    async getThumbnailAsync(id: Guid) {
        return await this.callAsync<string>("GetThumbnail", { id });
    }

    async clearCanvasAsync() {
        return await this.callAsync("ClearCanvasAsync", {});
    }

    async newGraffitiAsync() {
        return await this.callAsync("NewGraffitiAsync", {});
    }

    async saveGraffitiAsync(asNew: boolean) {
        return await this.callAsync<IGraffitiInfo>("SaveGraffitiAsync", { asNew });
    }

    async loadGraffitiAsync(id: Guid, originalPose: boolean) {
        return await this.callAsync("LoadGraffitiAsync", { id, originalPose });
    }

    async deleteGraffitiAsync(id: Guid) {
        return await this.callAsync("DeleteGraffiti", { id });
    }

    async shareColorImageAsync(id: Guid) {
        return await this.callAsync("ShareColorImageAsync", { graffitiId: id });
    }
}

export const appBridge = new Bridge();
