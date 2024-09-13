using XrEngine.Video.Abstraction;

namespace XrEngine.Video
{
    public struct SpsDecoder
    {
        static readonly int[] EXTRA_PROFILES = [100, 110, 122, 244, 44, 83, 118];

        int _pos;
        readonly byte[] _data;

        public SpsDecoder(byte[] data)
        {
            _data = data;
            _pos = 0;
        }

        void DecodeWork(ref VideoFormat format)
        {
            try
            {

                int forbidden_zero_bit = getU(1);
                int nal_ref_idc = getU(2);
                int nal_unit_type = getU(5);
                //END of NAL_header

                //Start of SPS data
                int profile_idc = getU(8);
                int constraint_set0_flag = getU(1);
                int constraint_set1_flag = getU(1);
                int constraint_set2_flag = getU(1);
                int constraint_set3_flag = getU(1);
                int constraint_set4_flag = getU(1);
                int constraint_set5_flag = getU(1);
                int reserved_zero_2bits = getU(2);

                int level_idc = getU(8);
                int seq_parameter_set_id = uev();

                if (EXTRA_PROFILES.Contains(profile_idc))
                {
                    int chroma_format_idc = getU(1);
                    if (chroma_format_idc == 3)
                        getU(1);
                    uev();
                    uev();
                    getU(1);
                    int sql_scaling_matrix_present_flag = getU(1);
                }

                int log2_max_frame_num_minus4 = uev();
                int pict_order_cnt_type = uev();

                if (pict_order_cnt_type == 0)
                {
                    uev();
                }
                else if (pict_order_cnt_type == 1)
                {
                    getU(1);
                    sev();
                    sev();
                    int n = uev();
                    for (int i = 0; i < n; i++)
                        sev();
                }
                int num_ref_frames = uev();
                getU(1);
                format.Width = (uev() + 1) * 16;
                format.Height = (uev() + 1) * 16;
                int frame_mbs_only_flag = getU(1);
                if (frame_mbs_only_flag == 0)
                {
                    int mb_adaptive_frame_flag = getU(1);
                }
                int direct_8x8_inference_flag = getU(1);
                int frame_cropping_flag = getU(1);
                if (frame_cropping_flag == 1)
                {
                    int frame_crop_left_offset = uev();
                    int frame_crop_right_offset = uev();
                    int frame_crop_top_offset = uev();
                    int frame_crop_bottom_offset = uev();
                    format.Width -= frame_crop_left_offset + frame_crop_right_offset;
                    format.Height -= frame_crop_top_offset + frame_crop_bottom_offset;
                }
            }
            catch
            {
            }
        }

        int ev(bool signed)
        {
            int bitcount = 0;

            while (getBit() == 0)
                bitcount++;

            int result = 1;
            for (int i = 0; i < bitcount; i++)
            {
                int b = getBit();
                result = result * 2 + b;
            }
            result--;
            if (signed)
                result = (result + 1) / 2 * (result % 2 == 0 ? -1 : 1);

            return result;
        }

        int uev()
        {
            return ev(false);
        }

        int sev()
        {
            return ev(true);
        }

        int getU(int bits)
        {
            int result = 0;
            for (int i = 0; i < bits; i++)
            {
                result = result * 2 + getBit();
            }
            return result;
        }

        int getBit()
        {
            int mask = 1 << (7 - (_pos & 7));
            int idx = _pos >> 3;
            _pos++;
            return ((_data[idx] & mask) == 0) ? 0 : 1;
        }

        public static void Decode(byte[] data, ref VideoFormat format)
        {
            var decoder = new SpsDecoder(data);
            decoder.DecodeWork(ref format);
        }
    }
}
