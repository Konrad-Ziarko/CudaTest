﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using SharpPcap;
using Zniffer.Levenshtein;
using System.Text;
using CustomExtensions;
using System.Threading;

namespace Zniffer {
    class Sniffer {

        public ObservableCollection<InterfaceClass> UsedInterfaces = new ObservableCollection<InterfaceClass>();
        private List<ICaptureDevice> devices = new List<ICaptureDevice>();
        private MainWindow window;
        //

        private static bool BackgroundThreadStop = false;
        private static object QueueLock = new object();
        private static List<RawCapture> PacketQueue = new List<RawCapture>();
        Thread backgroundThread;

        public void endQueueThread() {
            BackgroundThreadStop = true;
        }

        private void addNewInterface(InterfaceClass interfaceObj) {
            CaptureDeviceList _devices = CaptureDeviceList.Instance;

            ICaptureDevice device = null;
            // differentiate based upon types
            foreach (ICaptureDevice dev in _devices) {
                if (dev.ToString().Contains(interfaceObj.Addres)) {
                    device = dev;
                    device.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival);
                    devices.Add(device);
                    device.Open();
                    device.StartCapture();

                    break;
                }
            }
        }

        public Sniffer(MainWindow window, ref ObservableCollection<InterfaceClass> UsedInterfaces) {
            this.window = window;
            this.UsedInterfaces = UsedInterfaces;
            UsedInterfaces.CollectionChanged += UsedInterfaces_CollectionChanged;

            backgroundThread = new Thread(BackgroundThread);
            backgroundThread.Start();

        }

        private void UsedInterfaces_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            //Only if main does not overrides this event

        }

        internal void removeAllConnections() {
            foreach (var dev in devices) {
                dev.StopCapture();
                dev.Close();
            }
            devices = new List<ICaptureDevice>();
        }

        internal void addAllConnections() {
            foreach (var iFace in UsedInterfaces) {
                addNewInterface(iFace);
            }
        }

        private static void device_OnPacketArrival(object sender, CaptureEventArgs e) {
            lock (QueueLock) {
                PacketQueue.Add(e.Packet);
            }
        }


        private void BackgroundThread() {
            while (!BackgroundThreadStop) {
                bool shouldSleep = true;

                lock (QueueLock) {
                    if (PacketQueue.Count != 0) {
                        shouldSleep = false;
                    }
                }

                if (shouldSleep) {
                    Thread.Sleep(250);
                }
                else // should process the queue
                {
                    List<RawCapture> queue;
                    lock (QueueLock) {
                        // swap queues, giving the capture callback a new one
                        queue = PacketQueue;
                        PacketQueue = new List<RawCapture>();
                    }

                    Console.WriteLine("BackgroundThread: Queue.Count is {0}", queue.Count);

                    foreach (var packet in queue) {
                        var _packet = PacketDotNet.Packet.ParsePacket(packet.LinkLayerType, packet.Data);

                        if (_packet is PacketDotNet.EthernetPacket) {
                            var ip = (PacketDotNet.IpPacket)_packet.Extract(typeof(PacketDotNet.IpPacket));
                            if (ip != null) {
                                bool filterOut = true;
                                InterfaceClass tmpInterface = null;
                                foreach (InterfaceClass iFace in UsedInterfaces) {
                                    if (iFace.Addres.Equals(ip.DestinationAddress.ToString())) {
                                        filterOut = false;
                                        tmpInterface = iFace;
                                        break;
                                    }
                                }
                                if (filterOut == false) {
                                    filterOut = true;
                                    var tcp = (PacketDotNet.TcpPacket)_packet.Extract(typeof(PacketDotNet.TcpPacket));
                                    if (tcp != null) {
                                        if (tmpInterface.isPortValid(tcp.DestinationPort)) {
                                            string phrase = MainWindow.SearchPhrase;
                                            LevenshteinMatches matches = Encoding.UTF8.GetString(tcp.PayloadData).Levenshtein(phrase, mode: window.snifferMode);
                                            if (matches.hasMatches) {
                                                window.AddTextToNetworkBox(tmpInterface.Addres + ":" + tcp.DestinationPort + "\n");
                                                window.AddTextToNetworkBox(matches);
                                            }
                                        }
                                    }
                                    var udp = (PacketDotNet.UdpPacket)_packet.Extract(typeof(PacketDotNet.UdpPacket));
                                    if (udp != null) {
                                        if (tmpInterface.isPortValid(udp.DestinationPort)) {
                                            string phrase = MainWindow.SearchPhrase;
                                            LevenshteinMatches matches = Encoding.UTF8.GetString(udp.PayloadData).Levenshtein(phrase, mode: window.snifferMode);
                                            if (matches.hasMatches) {
                                                window.AddTextToNetworkBox(tmpInterface.Addres + ":" + udp.DestinationPort + "\n");
                                                window.AddTextToNetworkBox(matches);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
