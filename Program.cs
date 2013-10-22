using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

// This program searches a (67MB) data file that represents the contents of a
// programmed CPX00 NAND Flash memory device.  The memory device is typically
// an ST-Micro NAND512W3A2C.  This device provides 4096 blocks of byte-sized
// memory where each block is 16K-bytes plus 512 bytes spare.

// The CPX00 was designed with the Datalight(R) FlashFX Pro(TM) NAND Flash
// memory management device driver.  FlashFX manages bad blocks by setting
// aside (reserving) a number of blocks at the end of the device which are
// used for replacement purposes.  For a NAND512 device there are 96 blocks
// in the reserved area.

namespace FindE7s
{
    class Program
    {
        static bool WriteToFile = true; // treated as a const
        static FileStream fs_out = null;
        static StreamWriter sw = null;
        static ArrayList bad_block_table = new ArrayList();
        static ArrayList bad_block_table_visits = new ArrayList();

        /// The following functions provide the physical specifications of
        /// the NAND512 NAND Flash memory device.

        /// <summary>
        /// Provides the number of blocks per device
        /// </summary>
        static long NumberOfBlocks()
        {
            return 4096L;
        }

        /// <summary>
        /// Provides the number of pages per block
        /// </summary>
        static long PagesPerBlock()
        {
            return 32L;
        }

        /// <summary>
        /// Provides the number of bytes in the main area of each page
        /// </summary>
        static long BytesPerPage()
        {
            return 512L;
        }

        /// <summary>
        /// Provides the number of bytes in the spare area of each page
        /// </summary>
        static long BytesPerSpare()
        {
            return 16L;
        }

        /// <summary>
        /// Provides the number of bytes in a block (including spare areas)
        /// </summary>
        static long BytesPerBlock()
        {
            return PagesPerBlock() * (BytesPerPage() + BytesPerSpare());
        }

        /// <summary>
        /// Provides the byte location index (zero-based) of the bad block marker
        /// </summary>
        static long FactoryBadBlockMarkerLoc()
        {
            return BytesPerPage() + 5L;  // point to the 6th byte in the spare area
        }

        /// <summary>
        /// Look for pages filled with 'E7' bytes
        /// </summary>
        /// <param name="fs">An open file stream to read from</param>
        static void CheckForE7s(FileStream fs)
        {
            int char_in;
            int state_num = 0;
            int byte_count = 0;
            uint location = 0;
            uint start_loc = 0;
            bool run = true;

            fs.Seek(0, SeekOrigin.Begin);

            while (run)
            {
                #region WHILE-LOOP
                try
                {
                    char_in = fs.ReadByte();

                    if (-1 != char_in)  // Have we reached the end of the stream?
                    {
                        switch (state_num)
                        {
                            case 0:
                                if (0xE7 == char_in)
                                {
                                    start_loc = location;
                                    byte_count++;
                                    state_num = 1;
                                }
                                break;
                            case 1:
                            // fall through
                            case 2:
                            // fall through
                            case 3:
                            // fall through
                            case 4:
                            // fall through
                            case 5:
                                if (0xE7 == char_in)
                                {
                                    byte_count++;
                                    state_num++;
                                }
                                else
                                {
                                    byte_count = 0;
                                    state_num = 0;
                                }
                                break;
                            case 6:
                                if (0xE7 == char_in)
                                {
                                    Console.Write("{0:X8},", start_loc);
                                    if (WriteToFile) sw.Write("{0:X8},", start_loc);
                                    byte_count++;
                                    state_num = 7;
                                }
                                else
                                {
                                    byte_count = 0;
                                    state_num = 0;
                                }
                                break;
                            case 7:
                                if (0xE7 == char_in)
                                {
                                    byte_count++;
                                }
                                else
                                {
                                    Console.WriteLine("{0}", byte_count);
                                    if (WriteToFile) sw.WriteLine("{0}", byte_count);
                                    byte_count = 0;
                                    state_num = 0;
                                }
                                break;
                            default:
                                Console.WriteLine("CFE7 Internal error (state_num={0})", state_num);
                                byte_count = 0;
                                state_num = 0;
                                break;
                        }
                    }
                    else
                    {
                        run = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                    run = false;
                }
                location++;
                #endregion
            }

            if (5 < state_num)
            {
                Console.WriteLine("{0}", byte_count);
                if (WriteToFile) sw.WriteLine("{0}", byte_count);
            }
        }

        /// <summary>
        /// Look for areas filled with '00' bytes
        /// </summary>
        /// <param name="fs">An open file stream to read from</param>
        static void CheckForZeros(FileStream fs)
        {
            int char_in;
            int state_num = 0;
            int byte_count = 0;
            uint location = 0;
            uint start_loc = 0;
            bool run = true;

            fs.Seek(0, SeekOrigin.Begin);

            while (run)
            {
                #region WHILE-LOOP
                try
                {
                    char_in = fs.ReadByte();

                    if (-1 != char_in)  // Have we reached the end of the stream?
                    {
                        switch (state_num)
                        {
                            case 0:
                                if (0 == char_in)
                                {
                                    start_loc = location;
                                    byte_count++;
                                    state_num = 1;
                                }
                                break;
                            case 1:
                            // fall through
                            case 2:
                            // fall through
                            case 3:
                            // fall through
                            case 4:
                            // fall through
                            case 5:
                            // fall through
                            case 6:
                            // fall through
                            case 7:
                            // fall through
                            case 8:
                            // fall through
                            case 9:
                            // fall through
                            case 10:
                            // fall through
                            case 11:
                            // fall through
                            case 12:
                                if (0 == char_in)
                                {
                                    byte_count++;
                                    state_num++;
                                }
                                else
                                {
                                    byte_count = 0;
                                    state_num = 0;
                                }
                                break;
                            case 13:
                                if (0 != char_in)
                                {
                                    Console.WriteLine("Zeros @ {0:X8}, {1} bytes,", start_loc, byte_count);
                                    if (WriteToFile) sw.WriteLine("Zeros @ {0:X8}, {1} bytes,", start_loc, byte_count);
                                    byte_count = 0;
                                    state_num = 0;
                                }
                                else
                                {
                                    byte_count++;
                                }
                                break;
                            default:
                                Console.WriteLine("CFZ Internal error (state_num={0})", state_num);
                                byte_count = 0;
                                state_num = 0;
                                break;
                        }
                    }
                    else
                    {
                        run = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                    run = false;
                }

                location++;
                #endregion
            }

            if (12 < state_num)
            {
                Console.WriteLine("{0} bytes remaining", byte_count);
                if (WriteToFile) sw.WriteLine("{0} bytes remaining", byte_count);
            }
        }

        /// <summary>
        /// Look for areas filled with bytes that are an incrementing sequence
        /// </summary>
        /// <param name="fs">An open file stream to read from</param>
        static void CheckForSequencePattern(FileStream fs)
        {
            int char_in;
            int state_num = 0;
            int byte_count = 0;
            uint location = 0;
            uint start_loc = 0;
            int last_byte = 0;
            bool run = true;

            fs.Seek(0, SeekOrigin.Begin);

            while (run)
            {
                #region WHILE-LOOP
                try
                {
                    char_in = fs.ReadByte();

                    if (-1 != char_in)  // Have we reached the end of the stream?
                    {
                        switch (state_num)
                        {
                            case 0:
                                last_byte = char_in;
                                start_loc = location;
                                byte_count = 1;
                                state_num = 1;
                                break;
                            case 1:
                                if (char_in == (last_byte + 1))
                                {
                                    last_byte = char_in;
                                    byte_count++;
                                }
                                else
                                {
                                    if (byte_count > 8)
                                    {
                                        Console.WriteLine("Sequence @ {0:X8}, {1} bytes", start_loc, byte_count);
                                        if (WriteToFile) sw.WriteLine("Sequence @ {0:X8}, {1} bytes", start_loc, byte_count);
                                    }

                                    last_byte = char_in;
                                    start_loc = location;
                                    byte_count = 1;
                                }
                                break;
                            default:
                                Console.WriteLine("CFSP Internal error (state_num={0})", state_num);
                                byte_count = 0;
                                state_num = 0;
                                break;
                        }
                    }
                    else
                    {
                        run = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                    run = false;
                }

                location++;
                #endregion
            }
        }

        /// <summary>
        /// Search for bad-block markers in the file.
        /// </summary>
        /// <param name="fs">An open file stream to read from</param>
        static void FindBadBlocks(FileStream fs)
        {
            ////////////////////////////////////////////////////////////
            // Page size = 512 bytes + 16 spare bytes (528 bytes total)
            // Block size = 32 pages
            // Device size = 4096 blocks
            // Max allowed bad blocks = 80 blocks (factory spec)
            // Bad block marker found in the 6th byte of the first page
            //  of each block (a non-FF byte).
            ////////////////////////////////////////////////////////////

            int char_in;
            long block_index;
            long seek_offset;

            fs.Seek(0L, SeekOrigin.Begin);
            Console.WriteLine("Searching for bad block markers...");

            for (block_index = 0; block_index < NumberOfBlocks(); block_index++)
            {
                seek_offset = (block_index * BytesPerBlock()) + FactoryBadBlockMarkerLoc();
#if DEBUG
                Console.WriteLine("Checking block {0} @{1:X8}", block_index, seek_offset);

//                if (block_index == 4095)
//                    Console.WriteLine("**Breakpoint**");
#endif

                fs.Seek(seek_offset, SeekOrigin.Begin);
                try
                {
                    char_in = fs.ReadByte();

                    if (-1 != char_in)
                    {
                        if (0xFF != char_in)
                        {
                            bad_block_table.Add((UInt32)block_index);
                            bad_block_table_visits.Add(false);
                        }
                    }
                    else
                    {
                        Console.WriteLine("FBB: Unexpected End-Of-File found!");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("FBB: " + ex.Message.ToString());
                    break;
                }
            }

            Console.Write(" ({0}) Bad blocks found in file:", bad_block_table.Count);
            if (WriteToFile) sw.Write(" ({0}) Bad blocks found in file:", bad_block_table.Count);

            for (int ii = 0; ii < bad_block_table.Count; ii++)
            {
                Console.Write(" {0:X} ({0})", bad_block_table[ii]);
                if (WriteToFile) sw.Write(" {0:X} ({0})", bad_block_table[ii]);
            }

            Console.WriteLine("");
            if (WriteToFile) sw.WriteLine("");
        }

        /// <summary>
        /// Determines if the 32-bit value has an odd number of bits set
        /// </summary>
        /// <param name="num">
        /// Bit 31 is partiy bit
        /// </param>
        static bool isOddParity(uint num)
        {
            uint lclnum = num;
            uint parity = lclnum >> 31;
            uint xor_sum = 0;
            uint ii;

            for (ii = 0; ii < 31; ii++)
            {
                xor_sum ^= (lclnum & 0x1);
                lclnum = lclnum >> 1;
            }

            return (parity != xor_sum);
        }

        /// <summary>
        /// Determines if the 16-bit value has an odd number of bits set
        /// </summary>
        /// <param name="num">
        /// Bit 15 is partiy bit
        /// </param>
        static bool isOddParity(ushort num)
        {
            ushort lclnum = num;
            ushort parity = (ushort) (lclnum >> 15);
            ushort xor_sum = 0;
            uint ii;

            for (ii = 0; ii < 15; ii++)
            {
                xor_sum ^= (ushort) (lclnum & 0x1);
                lclnum = (ushort) (lclnum >> 1);
            }

            return (parity != xor_sum);
        }

        #region BBMHEADERSTRUCT
        /// <summary>
        /// FlashFX BBM Header Layout
        /// </summary>
        public struct BBM_Header
        {
            public byte[] sig;
            public UInt32[] num_blks;
            public UInt16[] blk_size;
            public UInt16 status;
            public UInt16 map_size;
            public UInt16 inprog_idx;
            public UInt16[] num_spares;
            public UInt16 rsvd;
            public UInt32[] map;
            public uint Count;  // number of replacements

            public BBM_Header(uint unused)
            {
                sig = new byte[8];
                num_blks = new UInt32[2];
                blk_size = new UInt16[2];
                num_spares = new UInt16[2];
                map = new UInt32[96];
                status = 0;
                map_size = 0;
                inprog_idx = 0;
                rsvd = 0;
                Count = 0;
            }
        }
        #endregion

        /// <summary>
        /// Examine bad-block replacement table
        /// </summary>
        /// <param name="fs">An open file stream to read from</param>
        static void CheckBadBlockHeaders(FileStream fs)
        {
            #region LOCALS
            const UInt32 BbmFreeSpareMarker = 0xFFFFFFFF;
            const UInt32 BbmHeaderMarker = 0x7FFFFFFF;
            const UInt32 BbmFactoryBadBlockMarker = 0x7FFFFFFE;
            const UInt32 BbmReservedMarker1 = 0x7FFFFFFD;
            const UInt32 BbmReservedMarker2 = 0x7FFFFFFC;
            int char_in;
            int state_num = 0;
            uint start_loc = 0;
            BBM_Header[] bbm_headers = new BBM_Header[2];
            int header_idx = 0;
            int map_idx = 0;
            int map_max_idx = 0;
            uint[] header_loc = {0,0};
            UInt32 marker = 0;
            int marker_cnt = 0;
            int num_headers = 0;
            ushort num_spares;
            bool retried = false;
            bool run = true;
            #endregion

            bbm_headers[0] = new BBM_Header(0);
            bbm_headers[1] = new BBM_Header(0);
            fs.Seek(0, SeekOrigin.Begin);
            ///////////////////////////////////////////////////////
            // Position stream pointer to within last 2.5% of file
            //
            long fileLength = fs.Length;
            long startPos = (fileLength * (1000 - 25)) / 1000;
            fs.Seek(startPos, SeekOrigin.Begin);

            while (run)
            {
            TryFileSearch:
                #region WHILE-LOOP
                try
                {
                    char_in = fs.ReadByte();

                    if (-1 != char_in)  // Have we reached the end of the stream?
                    {
                        switch (state_num)
                        {
                            #region SEARCH4BBMSIG
                            case 0:
                                /////////////////////////////////////////////
                                // Searching for BBM header signature bytes:
                                // “DB C0 95 77 7A 5C F7 2C”
                                /////////////////////////////////////////////
                                if (0xDB == char_in)
                                {
                                    bbm_headers[header_idx].sig[0] = (byte)char_in;
                                    start_loc = (uint)fs.Position - 1;
                                    state_num = 1;
                                }
                                break;
                            case 1:
                                bbm_headers[header_idx].sig[1] = (byte)char_in;
                                state_num = (0xC0 == char_in) ? 2 : 0;
                                break;
                            case 2:
                                bbm_headers[header_idx].sig[2] = (byte)char_in;
                                state_num = (0x95 == char_in) ? 3 : 0;
                                break;
                            case 3:
                                bbm_headers[header_idx].sig[3] = (byte)char_in;
                                state_num = (0x77 == char_in) ? 4 : 0;
                                break;
                            case 4:
                                bbm_headers[header_idx].sig[4] = (byte)char_in;
                                state_num = (0x7A == char_in) ? 5 : 0;
                                break;
                            case 5:
                                bbm_headers[header_idx].sig[5] = (byte)char_in;
                                state_num = (0x5C == char_in) ? 6 : 0;
                                break;
                            case 6:
                                bbm_headers[header_idx].sig[6] = (byte)char_in;
                                state_num = (0xF7 == char_in) ? 7 : 0;
                                break;
                            case 7:
                                if (0x2C == char_in)
                                {
                                    bbm_headers[header_idx].sig[7] = (byte)char_in;
                                    Console.WriteLine("Found BBM header at: {0:X8}, block # {1:X} ({1})", start_loc, start_loc / BytesPerBlock());
                                    if (WriteToFile) sw.WriteLine("Found BBM header at: {0:X8}, block {1:X} ({1})", start_loc, start_loc / BytesPerBlock());
                                    state_num = 8;
                                }
                                else
                                {
                                    state_num = 0;
                                }
                                break;
                            #endregion
                            #region DECODEBLOCKSTATS
                            case 8:
                                // Decode the Data Blocks entry (4 bytes)
                                bbm_headers[header_idx].num_blks[0] = (uint)char_in;
                                state_num = 9;
                                break;
                            case 9:
                                // ...decoding Data Blocks
                                bbm_headers[header_idx].num_blks[0] += ((uint)char_in * 0x100);
                                state_num = 10;
                                break;
                            case 10:
                                // ...decoding Data Blocks
                                bbm_headers[header_idx].num_blks[0] += ((uint)char_in * 0x10000);
                                state_num = 11;
                                break;
                            case 11:
                                // Finish decoding Data Blocks
                                bbm_headers[header_idx].num_blks[0] += ((uint)char_in * 0x1000000);
                                // Show the value and the parity
                                Console.WriteLine("Number of data blocks= {0:X8} ({0}), parity is {1}", (bbm_headers[header_idx].num_blks[0] & 0x7FFFFFFF), isOddParity(bbm_headers[header_idx].num_blks[0]) ? "Ok" : "bad!!");
                                Console.WriteLine(" Value for number of data blocks is {0}", (bbm_headers[header_idx].num_blks[0] == 0x80000fa0) ? "as expected" : "not as expected!!");
                                if (WriteToFile)
                                {
                                    sw.WriteLine("Number of data blocks= {0:X8} ({0}), parity is {1}", (bbm_headers[header_idx].num_blks[0] & 0x7FFFFFFF), isOddParity(bbm_headers[header_idx].num_blks[0]) ? "Ok" : "bad!!");
                                    sw.WriteLine(" Value for number of data blocks is {0}", (bbm_headers[header_idx].num_blks[0] == 0x80000fa0) ? "as expected" : "not as expected!!");
                                }
                                state_num = 12;
                                break;
                            case 12:
                                // Decode the Block Size entry (2 bytes)
                                bbm_headers[header_idx].blk_size[0] = (ushort)char_in;
                                state_num = 13;
                                break;
                            case 13:
                                // Finish decoding Block Size
                                bbm_headers[header_idx].blk_size[0] += (ushort)(char_in * 0x100);
                                // Show the value and the parity
                                Console.WriteLine("Data block size= {0:X4} ({0}), parity is {1}", (bbm_headers[header_idx].blk_size[0] & 0x7FFF), isOddParity(bbm_headers[header_idx].blk_size[0]) ? "Ok" : "bad!!");
                                Console.WriteLine(" Value for data block size is {0}", (bbm_headers[header_idx].blk_size[0] == 0x4000) ? "as expected" : "not as expected!!");
                                if (WriteToFile)
                                {
                                    sw.WriteLine("Data block size= {0:X4} ({0}), parity is {1}", (bbm_headers[header_idx].blk_size[0] & 0x7FFF), isOddParity(bbm_headers[header_idx].blk_size[0]) ? "Ok" : "bad!!");
                                    sw.WriteLine(" Value for data block size is {0}", (bbm_headers[header_idx].blk_size[0] == 0x4000) ? "as expected" : "not as expected!!");
                                }
                                state_num = 14;
                                break;
                            #endregion
                            #region DECODESTATUS
                            case 14:
                                // Decode the Status word
                                bbm_headers[header_idx].status = (ushort)char_in;
                                state_num = 15;
                                break;
                            case 15:
                                // Finish decoding Status word
                                bbm_headers[header_idx].status += (ushort)(char_in * 0x100);

                                if (0 == bbm_headers[header_idx].status)            // Low table ID
                                {
                                    if (0 == num_headers)
                                    {
                                        Console.WriteLine("This is the LOW table");
                                        if (WriteToFile) sw.WriteLine("This is the LOW table");
                                    }
                                    else
                                    {
                                        Console.WriteLine("Header status is BAD, the low table has already been located!!");
                                        if (WriteToFile) sw.WriteLine("Header status is BAD, the low table has already been located!!");
                                    }
                                }
                                else if (0xFFFF == bbm_headers[header_idx].status)  // High table ID
                                {
                                    if (0 == num_headers)
                                    {
                                        Console.WriteLine("Header status is BAD, the low table HAS NOT been located!!");
                                        if (WriteToFile) sw.WriteLine("Header status is BAD, the low table HAS NOT been located!!");
                                    }
                                    else
                                    {
                                        Console.WriteLine("This is the HIGH table");
                                        if (WriteToFile) sw.WriteLine("This is the HIGH table");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Header status is BAD, unknown value found ({0:X4})!!", bbm_headers[header_idx].status);
                                    if (WriteToFile) sw.WriteLine("Header status is BAD, unknown value found ({0:X4})!!", bbm_headers[header_idx].status);
                                }

                                header_loc[num_headers] = start_loc;
                                num_headers++;
                                state_num = 16;
                                break;
                            #endregion
                            #region DECODEBLOCKSTATSCOPY
                            case 16:
                                // Decode the Data Blocks entry (4 bytes)
                                bbm_headers[header_idx].num_blks[1] = (uint)char_in;
                                state_num = 17;
                                break;
                            case 17:
                                // ...decoding Data Blocks
                                bbm_headers[header_idx].num_blks[1] += ((uint)char_in * 0x100);
                                state_num = 18;
                                break;
                            case 18:
                                // ...decoding Data Blocks
                                bbm_headers[header_idx].num_blks[1] += ((uint)char_in * 0x10000);
                                state_num = 19;
                                break;
                            case 19:
                                // Finish decoding Data Blocks
                                bbm_headers[header_idx].num_blks[1] += ((uint)char_in * 0x1000000);
                                // Show the value and the parity
                                Console.WriteLine("Copy of number of data blocks= {0:X8} ({0}), parity is {1}", (bbm_headers[header_idx].num_blks[1] & 0x7FFFFFFF), isOddParity(bbm_headers[header_idx].num_blks[1]) ? "Ok" : "bad!!");
                                Console.WriteLine(" Value for copy of number of data blocks is {0}", (bbm_headers[header_idx].num_blks[1] == 0x80000fa0) ? "as expected" : "not as expected!!");
                                Console.WriteLine(" Number of blocks {0}", ((bbm_headers[header_idx].num_blks[1] & 0x7FFFFFFF) == (bbm_headers[header_idx].num_blks[1] & 0x7FFFFFFF)) ? "match" : "do not match!!");
                                if (WriteToFile)
                                {
                                    sw.WriteLine("Copy of number of data blocks= {0:X8} ({0}), parity is {1}", (bbm_headers[header_idx].num_blks[1] & 0x7FFFFFFF), isOddParity(bbm_headers[header_idx].num_blks[1]) ? "Ok" : "bad!!");
                                    sw.WriteLine(" Value for copy of number of data blocks is {0}", (bbm_headers[header_idx].num_blks[1] == 0x80000fa0) ? "as expected" : "not as expected!!");
                                    sw.WriteLine(" Number of blocks {0}", ((bbm_headers[header_idx].num_blks[0] & 0x7FFFFFFF) == (bbm_headers[header_idx].num_blks[1] & 0x7FFFFFFF)) ? "match" : "do not match!!");
                                }
                                state_num = 20;
                                break;
                            case 20:
                                // Decode the Block Size entry (2 bytes)
                                bbm_headers[header_idx].blk_size[1] = (ushort)char_in;
                                state_num = 21;
                                break;
                            case 21:
                                // Finish decoding Block Size
                                bbm_headers[header_idx].blk_size[1] += (ushort)(char_in * 0x100);
                                // Show the value and the parity
                                Console.WriteLine("Copy of data block size= {0:X4} ({0}), parity is {1}", (bbm_headers[header_idx].blk_size[1] & 0x7FFF), isOddParity(bbm_headers[header_idx].blk_size[1]) ? "Ok" : "bad!!");
                                Console.WriteLine(" Value for copy of data block size is {0}", (bbm_headers[header_idx].blk_size[1] == 0x4000) ? "as expected" : "not as expected!!");
                                Console.WriteLine(" Block sizes {0}", ((bbm_headers[header_idx].blk_size[0] & 0x7FFF) == (bbm_headers[header_idx].blk_size[1] & 0x7FFF)) ? "match" : "do not match!!");
                                if (WriteToFile)
                                {
                                    sw.WriteLine("Copy of data block size= {0:X4} ({0}), parity is {1}", (bbm_headers[header_idx].blk_size[1] & 0x7FFF), isOddParity(bbm_headers[header_idx].blk_size[1]) ? "Ok" : "bad!!");
                                    sw.WriteLine(" Value for copy of data block size is {0}", (bbm_headers[header_idx].blk_size[1] == 0x4000) ? "as expected" : "not as expected!!");
                                    sw.WriteLine(" Block sizes {0}", ((bbm_headers[header_idx].blk_size[0] & 0x7FFF) == (bbm_headers[header_idx].blk_size[1] & 0x7FFF)) ? "match" : "do not match!!");
                                }
                                state_num = 22;
                                break;
                            #endregion
                            #region DECODEMAPSIZE
                            case 22:
                                // Decode the Map Size (2 bytes)
                                bbm_headers[header_idx].map_size = (ushort)char_in;
                                state_num = 23;
                                break;
                            case 23:
                                bbm_headers[header_idx].map_size += (ushort)(char_in * 0x100);
                                Console.WriteLine("Map size= {0:X4} ({0}), {1}", bbm_headers[header_idx].map_size, (bbm_headers[header_idx].map_size == 512) ? "Ok" : "not Ok!!");
                                if (WriteToFile) sw.WriteLine("Map size= {0:X4} ({0}), {1}", bbm_headers[header_idx].map_size, (bbm_headers[header_idx].map_size == 512) ? "Ok" : "not Ok!!");
                                state_num = 24;
                                break;
                            #endregion
                            #region DECODEINPROGINDEX
                            case 24:
                                // Decode the In-progress index (2 bytes)
                                bbm_headers[header_idx].inprog_idx = (ushort)char_in;
                                state_num = 25;
                                break;
                            case 25:
                                bbm_headers[header_idx].inprog_idx += (ushort)(char_in * 0x100);
                                Console.WriteLine("In-progress index= {0:X4} ({0}), {1}", bbm_headers[header_idx].inprog_idx, (bbm_headers[header_idx].inprog_idx == 0xFFFF) ? "Ok" : "not Ok!!");
                                if (WriteToFile) sw.WriteLine("In-progress index= {0:X4} ({0}), {1}", bbm_headers[header_idx].inprog_idx, (bbm_headers[header_idx].inprog_idx == 0xFFFF) ? "Ok" : "not Ok!!");
                                state_num = 26;
                                break;
                            #endregion
                            #region DECODESPAREBLOCKS
                            case 26:
                                // Decode the Spare Blocks entry
                                bbm_headers[header_idx].num_spares[0] = (ushort)char_in;
                                state_num = 27;
                                break;
                            case 27:
                                bbm_headers[header_idx].num_spares[0] += (ushort)(char_in * 0x100);
                                num_spares = (ushort)(bbm_headers[header_idx].num_spares[0] & 0x7FFFU);
                                Console.WriteLine("Number of spare blocks= {0:X4} ({0}), parity is {1}",
                                                   num_spares,
                                                   isOddParity(bbm_headers[header_idx].num_spares[0]) ? "Ok" : "bad!!"
                                                 );
                                Console.WriteLine(" Value for spare blocks is {0}",
                                                   ((num_spares != 0) && (num_spares < 1000)) ? "as expected" : "not as expected!!"
                                                 );
                                if (WriteToFile)
                                {
                                    sw.WriteLine("Number of spare blocks= {0:X4} ({0}), parity is {1}",
                                                   num_spares,
                                                   isOddParity(bbm_headers[header_idx].num_spares[0]) ? "Ok" : "bad!!"
                                                );
                                    sw.WriteLine(" Value for spare blocks is {0}",
                                                   ((num_spares != 0) && (num_spares < 1000)) ? "as expected" : "not as expected!!"
                                                );
                                }
                                state_num = 28;
                                break;
                            case 28:
                                bbm_headers[header_idx].num_spares[1] = (ushort)char_in;
                                state_num = 29;
                                break;
                            case 29:
                                bbm_headers[header_idx].num_spares[1] += (ushort)(char_in * 0x100);
                                num_spares = (ushort)(bbm_headers[header_idx].num_spares[1] & 0x7FFFU);
                                Console.WriteLine("Copy of number of spare blocks= {0:X4} ({0}), parity is {1}",
                                                   num_spares,
                                                   isOddParity(bbm_headers[header_idx].num_spares[1]) ? "Ok" : "bad!!"
                                                 );
                                Console.WriteLine(" Value for copy of spare blocks is {0}",
                                                   ((num_spares != 0) && (num_spares < 1000)) ? "as expected" : "not as expected!!"
                                                 );
                                Console.WriteLine(" Number of blocks {0}", ((bbm_headers[header_idx].num_spares[0] & 0x7FFF) == (bbm_headers[header_idx].num_spares[1] & 0x7FFF)) ? "match" : "do not match!!");
                                if (WriteToFile)
                                {
                                    sw.WriteLine("Copy of number of spare blocks= {0:X4} ({0}), parity is {1}",
                                                   num_spares,
                                                   isOddParity(bbm_headers[header_idx].num_spares[1]) ? "Ok" : "bad!!"
                                                );
                                    sw.WriteLine(" Value for copy of spare blocks is {0}",
                                                   ((num_spares != 0) && (num_spares < 1000)) ? "as expected" : "not as expected!!"
                                                );
                                    sw.WriteLine(" Number of blocks {0}", ((bbm_headers[header_idx].num_spares[0] & 0x7FFF) == (bbm_headers[header_idx].num_spares[1] & 0x7FFF)) ? "match" : "do not match!!");
                                }
                                if (((bbm_headers[header_idx].num_spares[0] & 0x7FFFU) < 1000) && (bbm_headers[header_idx].num_spares[0] == bbm_headers[header_idx].num_spares[1]))
                                {
                                    map_max_idx = bbm_headers[header_idx].num_spares[0] & 0x7FFF;
                                }
                                else
                                {
                                    map_max_idx = 96;
                                }
                                map_idx = 0;
                                state_num = 30;
                                break;
                            #endregion
                            #region DECODERESERVED
                            case 30:
                                // Decode the Reserved entry (2 bytes)
                                bbm_headers[header_idx].rsvd = (ushort)char_in;
                                state_num = 31;
                                break;
                            case 31:
                                bbm_headers[header_idx].rsvd += (ushort)(char_in * 0x100);
                                Console.WriteLine("Reserved entry= {0:X4} ({0}), {1}", bbm_headers[header_idx].rsvd, (bbm_headers[header_idx].rsvd == 0xFFFF) ? "Ok" : "not Ok!!");
                                if (WriteToFile) sw.WriteLine("Reserved entry= {0:X4} ({0}), {1}", bbm_headers[header_idx].rsvd, (bbm_headers[header_idx].rsvd == 0xFFFF) ? "Ok" : "not Ok!!");
                                state_num = 32;
                                break;
                            #endregion
                            #region DECODEBLOCKMAP
                            case 32:
                                // Decode the block map entries (4 bytes for each entry).
                                marker = (marker / 0x100) + (0x1000000 * (UInt32)char_in);
                                marker_cnt++; // byte counter
                                
                                if (3 < marker_cnt)
                                {
                                    bbm_headers[header_idx].map[map_idx] = marker;
                                    map_idx++;

                                    if (map_idx >= map_max_idx)
                                    {
                                        Console.WriteLine("Map({0}) Entries:", header_idx);
                                        if (WriteToFile) sw.WriteLine("Map({0}) Entries:", header_idx);

                                        for (map_idx = 0; map_idx < map_max_idx; map_idx++)
                                        {
                                            #region MARKERCHECKS
                                            Console.Write("{0,2}: {1:X8} ", map_idx, bbm_headers[header_idx].map[map_idx]);
                                            if (WriteToFile) sw.Write("{0,2}: {1:X8} ", map_idx, bbm_headers[header_idx].map[map_idx]);

                                            if (BbmHeaderMarker == (ulong)bbm_headers[header_idx].map[map_idx])
                                            {
                                                Console.WriteLine("OK (BBM header block)");
                                                if (WriteToFile) sw.WriteLine("OK (BBM header block)");
                                            }
                                            else if (BbmFactoryBadBlockMarker == (ulong)bbm_headers[header_idx].map[map_idx])
                                            {
                                                Console.WriteLine("OK (factory bad block in spare area)");
                                                if (WriteToFile) sw.WriteLine("OK (factory bad block in spare area)");
                                                bbm_headers[header_idx].Count++;
                                            }
                                            else if ((BbmReservedMarker1 == (ulong)bbm_headers[header_idx].map[map_idx]) ||
                                                     (BbmReservedMarker2 == (ulong)bbm_headers[header_idx].map[map_idx]))
                                            {
                                                Console.WriteLine("RESERVED!!");
                                                if (WriteToFile) sw.WriteLine("RESERVED!!");
                                            }
                                            else if (BbmFreeSpareMarker == (ulong)bbm_headers[header_idx].map[map_idx])
                                            {
                                                Console.WriteLine("OK (Free spare block)");
                                                if (WriteToFile) sw.WriteLine("Free spare block");
                                            }
                                            else if (0UL != ((ulong)bbm_headers[header_idx].map[map_idx] & 0x80000000UL))
                                            {
                                                Console.WriteLine("OK");
                                                if (WriteToFile) sw.WriteLine("OK");
                                                bbm_headers[header_idx].Count++;
                                            }
                                            else
                                            {
                                                Console.WriteLine("Bad!!");
                                                if (WriteToFile) sw.WriteLine("Bad!!");
                                            }
                                            #endregion
                                        }

                                        if (header_idx == 1)
                                        {
                                            #region BBMBLOCKMAPCHECKS
                                            bool fMatch = false;
                                            uint distance = header_loc[1] - header_loc[0];

                                            run = false;
                                            Console.WriteLine("Distance between headers= {0} bytes, {1}", distance, (distance < 16900) ? "OK" : "Bad!!");
                                            if (WriteToFile) sw.WriteLine("Distance between headers= {0} bytes, {1}", distance, (distance < 16900) ? "OK" : "Bad!!");

                                            if ((bad_block_table.Count != bbm_headers[0].Count) ||
                                                (bad_block_table.Count != bbm_headers[1].Count) ||
                                                (bbm_headers[0].Count != bbm_headers[1].Count))
                                            {
                                                Console.WriteLine("*****************************************");
                                                Console.WriteLine("*        BADLY FORMED BBM HEADER        *");
                                                Console.WriteLine("*    The number of bad blocks do not    *");
                                                Console.WriteLine("* match the number of block map entries *");
                                                Console.WriteLine("*****************************************");
                                                Console.WriteLine("Bad block count= {0}", bad_block_table.Count);
                                                Console.WriteLine("BBM map #0 count= {0}", bbm_headers[0].Count);
                                                Console.WriteLine("BBM map #1 count= {0}", bbm_headers[1].Count);
                                                if (WriteToFile)
                                                {
                                                    sw.WriteLine("*****************************************");
                                                    sw.WriteLine("*        BADLY FORMED BBM HEADER        *");
                                                    sw.WriteLine("*    The number of bad blocks do not    *");
                                                    sw.WriteLine("* match the number of block map entries *");
                                                    sw.WriteLine("*****************************************");
                                                    sw.WriteLine("Bad block count= {0}", bad_block_table.Count);
                                                    sw.WriteLine("BBM map #0 count= {0}", bbm_headers[0].Count);
                                                    sw.WriteLine("BBM map #1 count= {0}", bbm_headers[1].Count);
                                                }
                                            }
                                            else
                                            {
                                                UInt32 marker2;
                                                int ii, jj;

                                                fMatch = true;  // for now...
                                                Console.WriteLine("BBM maps contain same number of entries ({0})", bbm_headers[0].Count);
                                                if (WriteToFile) sw.WriteLine("BBM maps contain same number of entries ({0})", bbm_headers[0].Count);
                                                ii = 1;

                                                for (map_idx=0; map_idx<map_max_idx; map_idx++)
                                                {
                                                    if ((BbmFreeSpareMarker != bbm_headers[0].map[map_idx]) &&          /* Map #0 checks */
                                                        (BbmHeaderMarker != bbm_headers[0].map[map_idx]) &&
                                                        (BbmFreeSpareMarker != bbm_headers[1].map[map_idx]) &&          /* Map #1 checks */
                                                        (BbmHeaderMarker != bbm_headers[1].map[map_idx]))
                                                    {
                                                        if (0 != (0x80000000 & bbm_headers[0].map[map_idx]) &&          /* Is there a block index? */
                                                            0 != (0x80000000 & bbm_headers[1].map[map_idx]))
                                                        {
                                                            marker = bbm_headers[0].map[map_idx] & 0x7FFFFFFF;          /* yes... */
                                                            marker2 = bbm_headers[1].map[map_idx] & 0x7FFFFFFF;

                                                            if (marker == marker2)                                    /* Do the two map entries match? */
                                                            {
                                                                if (bad_block_table.Contains(marker))                   /* Yes, is it also in the bad block table? */
                                                                {
                                                                    Console.WriteLine("{0,2}: Replaced block {1,3:X} found in bad block table ({2})", map_idx, marker, ii);
                                                                    if (WriteToFile) sw.WriteLine("{0,2}: Replaced block {1,3:X} found in bad block table ({2})", map_idx, marker, ii);

                                                                    jj = bad_block_table.IndexOf(marker);               /* Check off as found */
                                                                    bad_block_table_visits[jj] = true;
                                                                }
                                                                else
                                                                {
                                                                    Console.WriteLine("{0,2}: Replaced block {1,3:X} NOT found in bad block table!! ({2})", map_idx, marker, ii);
                                                                    if (WriteToFile) sw.WriteLine("{0,2}: Replaced block {1,3:X} NOT found in bad block table!! ({2})", map_idx, marker, ii);
                                                                    fMatch = false;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Console.WriteLine("{0,2}: BBM maps do not match; header #0 map contains= {1:X}, header #1 map contains= {2:X} ({3})", map_idx, marker, marker2, ii);
                                                                if (WriteToFile) sw.WriteLine("{0,2}: BBM maps do not match; header #0 map contains= {1:X}, header #1 map contains= {2:X} ({3})", map_idx, marker, marker2, ii);
                                                                fMatch = false;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (BbmFactoryBadBlockMarker == bbm_headers[0].map[map_idx] &&
                                                                BbmFactoryBadBlockMarker == bbm_headers[1].map[map_idx])
                                                            {
                                                                marker = (uint)(95 - map_idx) + 4000;                   /* Generate a spare area marker value */

                                                                if (bad_block_table.Contains(marker))                   /* Is it in the bad block table? */
                                                                {
                                                                    Console.WriteLine("{0,2}: Spare Area bad block {1,3:X} found in bad block table ({2})", map_idx, marker, ii);
                                                                    if (WriteToFile) sw.WriteLine("{0,2}: Spare Area bad block {1,3:X} found in bad block table ({2})", map_idx, marker, ii);

                                                                    jj = bad_block_table.IndexOf(marker);               /* Check off as found */
                                                                    bad_block_table_visits[jj] = true;
                                                                }
                                                                else
                                                                {
                                                                    Console.WriteLine("{0,2}: Spare Area bad block {1,3:X} NOT found in bad block table!! ({2})", map_idx, marker, ii);
                                                                    if (WriteToFile) sw.WriteLine("{0,2}: Spare Area bad block {1,3:X} NOT found in bad block table!! ({2})", map_idx, marker, ii);
                                                                    fMatch = false;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Console.WriteLine("{0,2}: BBM maps do not match; header #0 map contains= {1,3:X}, header #1 map contains= {2,3:X8} ({3})",
                                                                                   map_idx, bbm_headers[0].map[map_idx], bbm_headers[1].map[map_idx], ii
                                                                                 );
                                                                if (WriteToFile) sw.WriteLine("{0,2}: BBM maps do not match; header #0 map contains= {1,3:X}, header #1 map contains= {2,3:X8} ({3})",
                                                                                               map_idx, bbm_headers[0].map[map_idx], bbm_headers[1].map[map_idx], ii
                                                                                             );
                                                            }
                                                        }

                                                        ii++;
                                                    }
                                                }

                                                if (fMatch)
                                                {
                                                    Console.WriteLine("+----------------------------------+");
                                                    Console.WriteLine("|  Replacement map contents match  |");
                                                    Console.WriteLine("+----------------------------------+");
                                                    if (WriteToFile)
                                                    {
                                                        sw.WriteLine("+----------------------------------+");
                                                        sw.WriteLine("|  Replacement map contents match  |");
                                                        sw.WriteLine("+----------------------------------+");
                                                    }
                                                }
                                            }
                                            #endregion
                                        }

                                        state_num = 0;
                                        header_idx++;
                                    }
                                    
                                    marker_cnt = 0;
                                    marker = 0;
                                }
                                break;
                            #endregion
                            default:
                                Console.WriteLine("CBBH: Internal error (state_num={0})", state_num);
                                state_num = 0;
                                break;
                        }
                    }
                    else
                    {
                        if (false == retried)
                        {
                            if (state_num < 8)
                            {
                                Console.WriteLine("BBM header signature not found!");
                                if (WriteToFile) sw.WriteLine("BBM header signature not found!");
                                DialogResult rv = MessageBox.Show("Do you want to try again from the begining of the file?", "BBM Header Signature Not Found", MessageBoxButtons.RetryCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

                                if (DialogResult.Retry == rv)
                                {
                                    retried = true;
                                    fileLength = fs.Length;
                                    startPos = 0;
                                    fs.Seek(startPos, SeekOrigin.Begin);
                                    state_num = 0;
                                    goto TryFileSearch;
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("BBM header signature not found during retry!");
                            if (WriteToFile) sw.WriteLine("BBM header signature not found during retry!");
                        }
                        run = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("CBBH: " + ex.Message.ToString());
                    run = false;
                }
                #endregion
            }
        }

        /// <summary>
        /// The entry-point of the application.  This function collects the
        /// input filename and drives the testing of the file contents.
        /// </summary>
        /// <param name="args">The filename from the command-line (if available)</param>
        [STAThreadAttribute]
        static void Main(string[] args)
        {
            string strInFile = null;
            FileStream fs_in = null;
            bool run = true;

            if (0 < args.Length)    // Is a file name specified on the command line?
            {
                strInFile = args[0];
            }
            else
            {
                OpenFileDialog ofd = new OpenFileDialog();

                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;
                ofd.DefaultExt = "*.bin";
                ofd.Filter = "Bin files (*.bin)|*.bin|All files (*.*)|*.*";
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                ofd.Multiselect = false;
                ofd.ShowHelp = false;
                ofd.Title = "Open a NAND Flash Binary File";

                if (DialogResult.OK == ofd.ShowDialog())
                {
                    strInFile = ofd.FileName;
                }

                ofd.Dispose();
            }

            if ((null != strInFile) && (0 < strInFile.Length))
            {
                try
                {
#if DEBUG
                    Console.WriteLine("Opening input file: {0}", strInFile);
#endif
                    fs_in = new FileStream(strInFile, FileMode.Open, FileAccess.Read);

                    if (WriteToFile)
                    {
                        Assembly assem = Assembly.GetExecutingAssembly();
                        StringBuilder sbNameVers = new StringBuilder(" " + assem.GetName().Name);
                        string strOutFile = Path.GetFileNameWithoutExtension(strInFile) + "_out.txt";

                        strOutFile = Path.Combine(Path.GetDirectoryName(strInFile), strOutFile);
#if DEBUG
                        Console.WriteLine("Opening output file: {0}", strOutFile);
#endif
                        fs_out = new FileStream(strOutFile, FileMode.Create, FileAccess.Write);
                        sw = new StreamWriter(fs_out);
                        sbNameVers.AppendFormat(" version {0}.{1}", assem.GetName().Version.Major, assem.GetName().Version.Minor);
                        sw.WriteLine(sbNameVers.ToString());
                        sw.WriteLine(new String('=', sbNameVers.Length+1));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                    run = false;
                }

                if (run)
                {
#if DEBUG
                    Console.WriteLine("Scanning...");
#endif
                    FindBadBlocks(fs_in);
                    CheckBadBlockHeaders(fs_in);
                    //CheckForE7s(fs);
                    //CheckForZeros(fs);
                    //CheckForSequencePattern(fs);
                }

                if (null != fs_in)
                {
                    fs_in.Close();
                }

                if (null != sw)
                {
                    sw.Close();
                }
                else if (null != fs_out)
                {
                    fs_out.Close();
                }
#if DEBUG
                //MessageBox.Show("Done","Find E7s...");
                Console.WriteLine("Done, press <enter> to close window.");
                Console.ReadLine();
#endif
            }
        }
    }
}
