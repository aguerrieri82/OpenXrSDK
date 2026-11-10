export type Guid = string;

export interface IVector2 {
    x: number;
    y: number;
}

export interface IVector3 {
    x: number;
    y: number;
    z: number;
}

export interface IQuaternion {
    x: number;
    y: number;
    z: number;
    w: number;
}

export interface IPose3 {
    position: IVector3;
    orientation: IQuaternion;
}

export interface IGraffitiInfo {
    id: Guid;
    date: string;
    size: IVector2;
    texelSize: number;
    anchorId?: Guid;
    anchorPose: IPose3;
}
