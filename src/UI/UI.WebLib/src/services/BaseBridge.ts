
declare global {

    var bridge: {
        postMessage(msg: string) : void;
    }
}
 

interface IReceiveMsg {
    type: "response" | "error" |"call";
    reqId: string;
    result: unknown;
    args: unknown[];
    method: string;
}

interface ISendMsg {
    type: "call" | "response" | "error";
    reqId: string;
    args?: Record<string, unknown>;
    method: string;
    result: unknown;
}

interface IHandler {
   
    res: (obj: unknown) => void;
    rej: (obj: unknown) => void;
}

export class BaseBridge  {

    protected _handlers: Map<string, IHandler> = new Map();

    constructor() {

        if ("cefSharp" in window)
            window.bridge = window["cefSharp"] as any;

        window.addEventListener("message", ev => {

            if (typeof ev.data != "string")
                return;

            try {
                const msg = JSON.parse(ev.data) as IReceiveMsg;

                if (typeof msg == "object" && "type" in msg)
                    this.handleMessageAsync(msg);
            }
            catch {

            }
        });
    }

    start() {
        console.log("Bridge init");
    }


    protected async handleMessageAsync(msg: IReceiveMsg) {

        if (msg.type == "call") {

            const method = this[msg.method];

            try {

                if (!method) 
                    throw new Error("Method not found: " + msg.method); 

                const res = await method.apply(this, msg.args);

                bridge.postMessage(JSON.stringify({
                    type: "response",
                    reqId: msg.reqId,
                    result: res
                } as ISendMsg));
            }
            catch (ex) {

                bridge.postMessage(JSON.stringify({
                    type: "error",
                    reqId: msg.reqId,
                    result: (ex as Error)?.message
                } as ISendMsg));
            }
        }
        else {

            const handler = this._handlers.get(msg.reqId);

            if (!handler) {
                console.error("Handler not found for reqId: " + msg.reqId);
                return;
            }

            if (msg.type == "response") {
                handler.res(msg.result);
            }
            else if (msg.type == "error") {
                handler.rej(msg.result);
            }

            this._handlers.delete(msg.reqId);
        }
    }

    protected callAsync<T>(method: string, args: Record<string, unknown>) {

        if (!("bridge" in window))
            return;

        const reqId = new Date().getTime().toString();

        bridge.postMessage(JSON.stringify({
            type: "call",
            method: method,
            reqId: reqId,
            args
        } as ISendMsg));

        return new Promise<T>((res, rej) => {
            this._handlers.set(reqId, {
                res,
                rej
            });
        });

    }

}