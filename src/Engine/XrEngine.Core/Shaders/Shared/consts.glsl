#define ALPHA_OPAQUE      0x01
#define ALPHA_BLEND       0x02
#define ALPHA_MASK        (0x04 | ALPHA_OPAQUE)
#define ALPHA_BLEND_MAIN  (0x08 | ALPHA_BLEND | ALPHA_OPAQUE)
#define ALPHA_ADD         (ALPHA_BLEND | 0x10)
#define ALPHA_MIN         (ALPHA_BLEND | 0x20)
#define ALPHA_MAX         (ALPHA_BLEND | 0x40)
#define ALPHA_PUNCH       (ALPHA_BLEND | 0x80)
#define ALPHA_OVER        (ALPHA_BLEND | 0x100)