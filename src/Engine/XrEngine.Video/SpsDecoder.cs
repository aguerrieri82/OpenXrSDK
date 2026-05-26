using XrEngine.Media;

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

                var forbidden_zero_bit = getU(1);
                var nal_ref_idc = getU(2);
                var nal_unit_type = getU(5);
                //END of NAL_header

                //Start of SPS data
                var profile_idc = getU(8);
                var constraint_set0_flag = getU(1);
                var constraint_set1_flag = getU(1);
                var constraint_set2_flag = getU(1);
                var constraint_set3_flag = getU(1);
                var constraint_set4_flag = getU(1);
                var constraint_set5_flag = getU(1);
                var reserved_zero_2bits = getU(2);

                var level_idc = getU(8);
                var seq_parameter_set_id = uev();

                if (EXTRA_PROFILES.Contains(profile_idc))
                {
                    var chroma_format_idc = getU(1);
                    if (chroma_format_idc == 3)
                        getU(1);
                    uev();
                    uev();
                    getU(1);
                    var sql_scaling_matrix_present_flag = getU(1);
                }

                var log2_max_frame_num_minus4 = uev();
                var pict_order_cnt_type = uev();

                if (pict_order_cnt_type == 0)
                {
                    uev();
                }
                else if (pict_order_cnt_type == 1)
                {
                    getU(1);
                    sev();
                    sev();
                    var n = uev();
                    for (var i = 0; i < n; i++)
                        sev();
                }
                var num_ref_frames = uev();
                getU(1);
                format.Width = (uev() + 1) * 16;
                format.Height = (uev() + 1) * 16;
                var frame_mbs_only_flag = getU(1);
                if (frame_mbs_only_flag == 0)
                {
                    var mb_adaptive_frame_flag = getU(1);
                }
                var direct_8x8_inference_flag = getU(1);
                var frame_cropping_flag = getU(1);
                if (frame_cropping_flag == 1)
                {
                    var frame_crop_left_offset = uev();
                    var frame_crop_right_offset = uev();
                    var frame_crop_top_offset = uev();
                    var frame_crop_bottom_offset = uev();
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
            var bitcount = 0;

            while (getBit() == 0)
                bitcount++;

            var result = 1;
            for (var i = 0; i < bitcount; i++)
            {
                var b = getBit();
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
            var result = 0;
            for (var i = 0; i < bits; i++)
            {
                result = result * 2 + getBit();
            }
            return result;
        }

        int getBit()
        {
            var mask = 1 << (7 - (_pos & 7));
            var idx = _pos >> 3;
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
