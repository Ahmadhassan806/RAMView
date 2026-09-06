import { useState, useEffect, useMemo, useRef } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  computeSquarifiedTreemap,
  formatBytes,
  type ProcessItem
} from './treemap';
import { getAppLogo } from './logos';
import {
  Download,
  Terminal,
  Maximize2,
  Minimize2,
  Search,
  Layers,
  Cpu,
  ShieldCheck,
  Zap,
  HardDrive,
  X,
  Menu,
  Check,
  Copy,
  AlertTriangle,
  Info,
  ExternalLink,
  Laptop,
  Radio,
  Mail,
  Phone
} from 'lucide-react';

// Fallback authentic process profile if local live agent is offline
const SIMULATED_PROCESSES: ProcessItem[] = [
  { id: 101, name: 'Google Chrome', workingSetBytes: 3450 * 1024 * 1024, instanceCount: 14, category: 'app', icon: 'chrome', path: 'C:\\Program Files\\Google\\Chrome\\chrome.exe' },
  { id: 102, name: 'VS Code', workingSetBytes: 1840 * 1024 * 1024, instanceCount: 6, category: 'app', icon: 'code', path: 'C:\\Users\\User\\AppData\\Local\\Programs\\VS Code\\Code.exe' },
  { id: 103, name: 'Discord', workingSetBytes: 890 * 1024 * 1024, instanceCount: 4, category: 'app', icon: 'discord', path: 'C:\\Users\\User\\AppData\\Local\\Discord\\Discord.exe' },
  { id: 104, name: 'Spotify', workingSetBytes: 620 * 1024 * 1024, instanceCount: 3, category: 'app', icon: 'spotify', path: 'C:\\Users\\User\\AppData\\Roaming\\Spotify\\Spotify.exe' },
  { id: 105, name: 'explorer.exe', workingSetBytes: 480 * 1024 * 1024, instanceCount: 1, category: 'system', icon: 'explorer', path: 'C:\\Windows\\explorer.exe' },
  { id: 106, name: 'dwm.exe', workingSetBytes: 380 * 1024 * 1024, instanceCount: 1, category: 'system', icon: 'dwm', path: 'C:\\Windows\\System32\\dwm.exe' },
  { id: 107, name: 'Steam', workingSetBytes: 340 * 1024 * 1024, instanceCount: 2, category: 'app', icon: 'steam', path: 'C:\\Program Files (x86)\\Steam\\steam.exe' },
  { id: 108, name: 'Slack', workingSetBytes: 780 * 1024 * 1024, instanceCount: 5, category: 'app', icon: 'slack', path: 'C:\\Users\\User\\AppData\\Local\\slack\\slack.exe' },
  { id: 109, name: 'Docker', workingSetBytes: 2150 * 1024 * 1024, instanceCount: 2, category: 'app', icon: 'docker', path: 'C:\\Program Files\\Docker\\Docker.exe' },
  { id: 110, name: 'System', workingSetBytes: 290 * 1024 * 1024, instanceCount: 1, category: 'system', icon: 'system', path: 'Kernel Memory' },
  { id: 111, name: 'svchost.exe', workingSetBytes: 420 * 1024 * 1024, instanceCount: 18, category: 'system', icon: 'svchost', path: 'C:\\Windows\\System32\\svchost.exe' },
  { id: 112, name: 'Notion', workingSetBytes: 520 * 1024 * 1024, instanceCount: 3, category: 'app', icon: 'notion', path: 'C:\\Users\\User\\AppData\\Local\\Programs\\Notion\\Notion.exe' },
  { id: 113, name: 'Windows Terminal', workingSetBytes: 190 * 1024 * 1024, instanceCount: 2, category: 'app', icon: 'terminal', path: 'C:\\Windows\\System32\\WindowsTerminal.exe' }
];

export function App() {
  // State
  const [processes, setProcesses] = useState<ProcessItem[]>(SIMULATED_PROCESSES);
  const [totalPhysicalRAM, setTotalPhysicalRAM] = useState<number>(16 * 1024 * 1024 * 1024);
  const [usedPhysicalRAM, setUsedPhysicalRAM] = useState<number>(12160 * 1024 * 1024);
  const [isLiveMachineConnected, setIsLiveMachineConnected] = useState<boolean>(false);
  const [isGrouped, setIsGrouped] = useState<boolean>(true);
  const [filterPreset, setFilterPreset] = useState<'all' | 'app' | 'system' | 'highest'>('all');
  const [searchTerm, setSearchTerm] = useState<string>('');
  const [selectedProcess, setSelectedProcess] = useState<ProcessItem | null>(null);
  const [mode, setMode] = useState<'overlay' | 'compact'>('overlay');
  const [toastMessage, setToastMessage] = useState<string | null>(null);
  const [modalType, setModalType] = useState<'requirements' | 'privacy' | 'changelog' | null>(null);
  const [mobileMenuOpen, setMobileMenuOpen] = useState<boolean>(false);
  const [isTerminatingRestricted, setIsTerminatingRestricted] = useState<boolean>(false);

  const canvasRef = useRef<HTMLDivElement>(null);
  const [canvasDim, setCanvasDim] = useState({ width: 720, height: 460 });

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3000);
  };

  // Poll Local REST API bridge for real-time computer RAM metrics
  useEffect(() => {
    let isMounted = true;
    const checkLocalAgent = async () => {
      try {
        const res = await fetch('http://127.0.0.1:51888/api/snapshot', {
          signal: AbortSignal.timeout(1500)
        });
        if (res.ok) {
          const data = await res.json();
          if (isMounted) {
            setIsLiveMachineConnected(true);
            setTotalPhysicalRAM(data.totalPhysicalBytes);
            setUsedPhysicalRAM(data.usedPhysicalBytes);
            if (Array.isArray(data.processes) && data.processes.length > 0) {
              const mapped: ProcessItem[] = data.processes.map((p: any) => ({
                id: p.processId || Math.floor(Math.random() * 10000),
                name: p.processName || 'Unknown',
                workingSetBytes: p.workingSet64 || 50 * 1024 * 1024,
                instanceCount: p.instanceCount || 1,
                category: isSystemProc(p.processName) ? 'system' : 'app',
                icon: p.processName,
                path: p.executablePath || undefined
              }));
              setProcesses(mapped);
            }
          }
          return;
        }
      } catch {
        if (isMounted) {
          setIsLiveMachineConnected(false);
        }
      }
    };

    checkLocalAgent();
    const interval = setInterval(checkLocalAgent, 1200);
    return () => {
      isMounted = false;
      clearInterval(interval);
    };
  }, []);

  // Subtle natural simulation fluctuations when agent is offline
  useEffect(() => {
    if (isLiveMachineConnected) return;
    const interval = setInterval(() => {
      setProcesses(prev =>
        prev.map(p => {
          const delta = (Math.random() - 0.49) * 0.03;
          const newBytes = Math.max(25 * 1024 * 1024, Math.round(p.workingSetBytes * (1 + delta)));
          return { ...p, workingSetBytes: newBytes };
        })
      );
      setUsedPhysicalRAM(prev => {
        const jitter = (Math.random() - 0.5) * (30 * 1024 * 1024);
        return Math.min(totalPhysicalRAM * 0.95, Math.max(totalPhysicalRAM * 0.2, prev + jitter));
      });
    }, 1500);
    return () => clearInterval(interval);
  }, [isLiveMachineConnected, totalPhysicalRAM]);

  // Window resize observer
  useEffect(() => {
    const updateSize = () => {
      if (canvasRef.current) {
        setCanvasDim({
          width: canvasRef.current.clientWidth,
          height: canvasRef.current.clientHeight
        });
      }
    };
    updateSize();
    window.addEventListener('resize', updateSize);
    return () => window.removeEventListener('resize', updateSize);
  }, [mode]);

  // Filter processes
  const filteredProcesses = useMemo(() => {
    let list = [...processes];
    if (filterPreset === 'app') {
      list = list.filter(p => p.category === 'app');
    } else if (filterPreset === 'system') {
      list = list.filter(p => p.category === 'system');
    } else if (filterPreset === 'highest') {
      list.sort((a, b) => b.workingSetBytes - a.workingSetBytes);
      list = list.slice(0, 8);
    }
    return list;
  }, [processes, filterPreset]);

  // Compute treemap geometry
  const treemapRects = useMemo(() => {
    return computeSquarifiedTreemap(
      filteredProcesses,
      canvasDim.width,
      canvasDim.height,
      20 * 1024 * 1024,
      0.60,
      5
    );
  }, [filteredProcesses, canvasDim]);

  const visibleMatches = useMemo(() => {
    if (!searchTerm.trim()) return treemapRects;
    return treemapRects.filter(r =>
      r.process.name.toLowerCase().includes(searchTerm.toLowerCase().trim())
    );
  }, [treemapRects, searchTerm]);

  const ramPercent = ((usedPhysicalRAM / totalPhysicalRAM) * 100).toFixed(1);

  const copyInstallCommand = () => {
    navigator.clipboard.writeText('powershell -ExecutionPolicy Bypass -File .\\installer\\Install-RAMView.ps1');
    showToast('Copied installation command to clipboard.');
  };

  const resetView = () => {
    setSearchTerm('');
    setFilterPreset('all');
    setSelectedProcess(null);
    setMode('overlay');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  return (
    <div className="min-h-screen bg-[#FAF7F2] text-[#1C1917] font-sans selection:bg-[#E7E2D9] selection:text-[#1C1917] overflow-x-hidden flex flex-col">
      {/* Toast Notification */}
      <AnimatePresence>
        {toastMessage && (
          <motion.div
            initial={{ opacity: 0, y: -20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -20 }}
            className="fixed top-5 left-1/2 -translate-x-1/2 z-50 px-4 py-2 rounded-xl bg-[#1C1917] text-[#FAF7F2] text-xs font-light tracking-wide shadow-xl flex items-center gap-2 border border-stone-700"
          >
            <Check className="w-3.5 h-3.5 text-emerald-400" />
            <span>{toastMessage}</span>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Header Navigation (Medium Dark with Shadows) */}
      <header className="border-b border-black/25 bg-[#201F24]/95 backdrop-blur-md sticky top-0 z-40 px-4 sm:px-8 py-3.5 shadow-[0_10px_30px_rgba(0,0,0,0.25)]">
        <div className="max-w-6xl mx-auto flex items-center justify-between">
          {/* Logo (Clickable) */}
          <button
            onClick={resetView}
            aria-label="RAM VIEW Home and Reset"
            className="flex items-center gap-3 text-left group focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/40 rounded-lg p-1"
          >
            <div className="w-8 h-8 rounded-lg bg-white/10 border border-white/15 flex items-center justify-center text-white group-hover:scale-105 transition-transform shadow-inner">
              <Cpu className="w-4 h-4" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="font-light italic tracking-tight text-lg text-white">
                  RAM VIEW
                </span>
                <span className="px-1.5 py-0.5 rounded text-[10px] font-medium bg-white/10 text-stone-300 border border-white/15">
                  v1.0.0
                </span>
              </div>
              <p className="text-[11px] text-stone-400 font-light italic hidden sm:block">Windows Desktop Memory Visualizer</p>
            </div>
          </button>

          {/* Desktop Navigation Links (Italic font-light) */}
          <nav className="hidden md:flex items-center gap-7 text-xs font-light italic text-stone-300">
            <a href="#live-hud" className="hover:text-white transition-colors">Visualizer</a>
            <a href="#features" className="hover:text-white transition-colors">Architecture</a>
            <a href="#specs" className="hover:text-white transition-colors">Specifications</a>
            <a href="#download" className="hover:text-white transition-colors">Download</a>
          </nav>

          {/* Desktop Actions */}
          <div className="hidden sm:flex items-center gap-3">
            <a
              href="/downloads/Install-RAMView.ps1"
              download
              onClick={() => showToast('Installer script download started.')}
              className="flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg bg-white/10 hover:bg-white/15 border border-white/15 text-stone-200 text-xs font-light italic transition"
            >
              <Terminal className="w-3.5 h-3.5 text-stone-300" />
              Install Script
            </a>

            <a
              href="#download"
              className="flex items-center gap-2 px-4 py-1.5 rounded-lg bg-[#FAF7F2] hover:bg-white text-[#1C1917] text-xs font-medium transition active:scale-95 shadow-md"
            >
              <Download className="w-3.5 h-3.5" />
              Download .EXE
            </a>
          </div>

          {/* Mobile Menu Button */}
          <button
            onClick={() => setMobileMenuOpen(prev => !prev)}
            aria-label="Toggle Mobile Navigation"
            className="md:hidden p-2 rounded-lg bg-white/10 border border-white/15 text-stone-200 hover:text-white focus:outline-none"
          >
            {mobileMenuOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
          </button>
        </div>
      </header>

      {/* Mobile Menu Drawer (Medium Dark with Shadows) */}
      <AnimatePresence>
        {mobileMenuOpen && (
          <motion.div
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: 'auto' }}
            exit={{ opacity: 0, height: 0 }}
            className="md:hidden border-b border-black/30 bg-[#201F24] px-6 py-4 space-y-3 z-30 shadow-[0_15px_30px_rgba(0,0,0,0.3)]"
          >
            <nav className="flex flex-col space-y-2 text-sm font-light italic">
              <a
                href="#live-hud"
                onClick={() => setMobileMenuOpen(false)}
                className="py-1 text-stone-300 hover:text-white"
              >
                Visualizer
              </a>
              <a
                href="#features"
                onClick={() => setMobileMenuOpen(false)}
                className="py-1 text-stone-300 hover:text-white"
              >
                Architecture
              </a>
              <a
                href="#specs"
                onClick={() => setMobileMenuOpen(false)}
                className="py-1 text-stone-300 hover:text-white"
              >
                Specifications
              </a>
              <a
                href="#download"
                onClick={() => setMobileMenuOpen(false)}
                className="py-1 text-stone-300 hover:text-white"
              >
                Download
              </a>
              <a
                href="#contact"
                onClick={() => setMobileMenuOpen(false)}
                className="py-1 text-stone-300 hover:text-white"
              >
                Support
              </a>
            </nav>
            <div className="pt-3 border-t border-white/10 flex flex-col gap-2">
              <a
                href="#download"
                onClick={() => setMobileMenuOpen(false)}
                className="w-full py-2 rounded-lg bg-[#FAF7F2] text-center font-medium text-xs text-[#1C1917] shadow-md"
              >
                Download for Windows
              </a>
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Main Container */}
      <main className="max-w-6xl mx-auto px-4 sm:px-8 py-8 sm:py-10 flex-1 w-full">
        {/* Live Hardware Status Ribbon */}
        <div className="mb-6">
          <div
            className={`p-3.5 rounded-xl border flex flex-wrap items-center justify-between gap-3 text-xs ${
              isLiveMachineConnected
                ? 'bg-[#EBF7F0] border-[#B7E5C7] text-[#166534]'
                : 'bg-[#F2EFE9] border-[#DDD7CD] text-[#57534E]'
            }`}
          >
            <div className="flex items-center gap-2.5">
              <div className="relative flex items-center justify-center">
                <span className={`w-2.5 h-2.5 rounded-full absolute animate-ping ${isLiveMachineConnected ? 'bg-emerald-400' : 'bg-stone-400'}`} />
                <span className={`w-2.5 h-2.5 rounded-full relative ${isLiveMachineConnected ? 'bg-emerald-600' : 'bg-stone-500'}`} />
              </div>
              <div>
                <span className="font-medium text-xs">
                  {isLiveMachineConnected
                    ? 'Connected to Local PC — Streaming Real Hardware Memory'
                    : 'Simulation Mode Active'}
                </span>
                <p className="text-[11px] opacity-80 mt-0.5 font-light italic">
                  {isLiveMachineConnected
                    ? `Live stream active from localhost:51888. Visualizing ${processes.length} actual Windows processes on your system.`
                    : 'Launch RAM VIEW locally on your PC to stream your live computer RAM directly.'}
                </p>
              </div>
            </div>

            <div className="flex items-center gap-2">
              {!isLiveMachineConnected && (
                <a
                  href="#download"
                  className="px-3 py-1 rounded-lg bg-[#1C1917] hover:bg-[#292524] text-[#FAF7F2] font-medium text-[11px] transition"
                >
                  Launch on PC
                </a>
              )}
              <button
                onClick={() => {
                  if (isLiveMachineConnected) {
                    showToast('Active stream connected to your computer.');
                  } else {
                    showToast('Checking localhost:51888...');
                  }
                }}
                className="px-3 py-1 rounded-lg bg-white/60 hover:bg-white border border-[#DDD7CD] text-[#292524] text-[11px] font-medium transition"
              >
                {isLiveMachineConnected ? 'Stream Active' : 'Refresh Agent'}
              </button>
            </div>
          </div>
        </div>

        {/* Hero Title (Thinner and Italic) */}
        <section id="live-hud" className="text-center mb-8">
          <motion.h1
            initial={{ opacity: 0, y: -8 }}
            animate={{ opacity: 1, y: 0 }}
            className="text-3xl sm:text-5xl font-light italic text-[#1C1917] tracking-tight leading-tight"
          >
            Live Squarified Treemap RAM Visualizer
          </motion.h1>
          <p className="text-xs sm:text-sm text-[#78716C] font-light italic mt-2.5 max-w-2xl mx-auto leading-relaxed">
            Real-time physical hardware memory mapped into translucent white boxes, sized proportionally to Working Set memory with official application identities.
          </p>
        </section>

        {/* HUD Window Container (Professional Charcoal Card on Cream Canvas) */}
        <motion.div
          layout
          className="relative rounded-2xl bg-[#141416] border border-[#27272A] p-4 sm:p-6 shadow-[0_25px_60px_rgba(0,0,0,0.25)] backdrop-blur-xl"
        >
          {/* Top HUD Bar */}
          <div className="flex flex-wrap items-center justify-between gap-4 pb-4 border-b border-white/10">
            {/* Live RAM Metric Gauge */}
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-lg bg-white/5 border border-white/10 text-stone-300">
                <Laptop className="w-4 h-4" />
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <span className="text-sm font-light text-white tracking-wide">
                    {formatBytes(usedPhysicalRAM)} <span className="text-stone-400 font-light italic">/ {formatBytes(totalPhysicalRAM)}</span>
                  </span>
                  <span className="text-xs text-stone-300 font-light italic">• {ramPercent}% Used</span>
                </div>
                {/* Visual RAM Meter (Sophisticated Platinum/Stone Gradient) */}
                <div className="w-40 sm:w-56 h-1.5 bg-white/10 rounded-full overflow-hidden mt-1.5">
                  <motion.div
                    className="h-full bg-gradient-to-r from-stone-400 to-white rounded-full"
                    style={{ width: `${Math.min(100, parseFloat(ramPercent))}%` }}
                    transition={{ ease: 'easeOut', duration: 0.3 }}
                  />
                </div>
              </div>
            </div>

            {/* Interactive Search & Mode Controls */}
            <div className="flex items-center gap-2 flex-wrap">
              {/* Search Box */}
              <div className="relative">
                <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-stone-400" />
                <input
                  type="text"
                  placeholder="Filter processes..."
                  value={searchTerm}
                  onChange={e => setSearchTerm(e.target.value)}
                  aria-label="Filter processes"
                  className="pl-8 pr-7 py-1 text-xs bg-white/5 border border-white/15 rounded-lg text-white placeholder-stone-500 focus:outline-none focus:border-stone-400 focus:bg-white/10 w-36 sm:w-48 transition font-light"
                />
                {searchTerm && (
                  <button
                    onClick={() => setSearchTerm('')}
                    aria-label="Clear search"
                    className="absolute right-2 top-1/2 -translate-y-1/2 text-stone-400 hover:text-white"
                  >
                    <X className="w-3 h-3" />
                  </button>
                )}
              </div>

              {/* Mode Toggle */}
              <button
                onClick={() => {
                  setMode(m => (m === 'overlay' ? 'compact' : 'overlay'));
                  showToast(mode === 'overlay' ? 'Switched to Compact Strip Mode' : 'Switched to Overlay HUD Mode');
                }}
                title="Toggle Mode"
                aria-label="Toggle Layout Mode"
                className="p-1.5 rounded-lg bg-white/5 hover:bg-white/10 border border-white/10 text-stone-300 transition hover:text-white active:scale-95"
              >
                {mode === 'overlay' ? <Minimize2 className="w-4 h-4" /> : <Maximize2 className="w-4 h-4" />}
              </button>

              {/* Simulation / Agent Indicator */}
              <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-white/5 border border-white/10 text-xs text-stone-300">
                <Radio className={`w-3.5 h-3.5 ${isLiveMachineConnected ? 'text-emerald-400 animate-pulse' : 'text-stone-400'}`} />
                <span className="hidden sm:inline font-light italic">{isLiveMachineConnected ? 'Live PC' : 'Simulation'}</span>
              </div>
            </div>
          </div>

          {/* Treemap Canvas Container */}
          <div
            ref={canvasRef}
            className={`relative w-full rounded-xl overflow-hidden bg-[#0C0C0E] border border-white/10 my-4 transition-all ${
              mode === 'overlay' ? 'h-[380px] sm:h-[460px]' : 'h-[80px]'
            }`}
          >
            <AnimatePresence>
              {mode === 'overlay' ? (
                visibleMatches.length > 0 ? (
                  treemapRects.map(r => {
                    const isMatch =
                      !searchTerm.trim() ||
                      r.process.name.toLowerCase().includes(searchTerm.toLowerCase().trim());

                    return (
                      <motion.div
                        key={r.process.id}
                        layout
                        initial={{ opacity: 0, scale: 0.95 }}
                        animate={{
                          opacity: isMatch ? 1 : 0.18,
                          x: r.x,
                          y: r.y,
                          width: r.width,
                          height: r.height
                        }}
                        exit={{ opacity: 0, scale: 0.9 }}
                        transition={{
                          type: 'spring',
                          stiffness: 280,
                          damping: 30,
                          mass: 0.8
                        }}
                        onClick={() => setSelectedProcess(r.process)}
                        style={{ position: 'absolute', top: 0, left: 0 }}
                        className="group cursor-pointer rounded-xl bg-white/[0.10] hover:bg-white/[0.22] border border-white/20 hover:border-white/55 p-2 sm:p-2.5 flex flex-col justify-between overflow-hidden shadow-sm backdrop-blur-md transition-colors"
                      >
                        {/* Box Top Header: Name & Instance Badge (only when space permits) */}
                        {r.height >= 55 && r.width >= 60 && (
                          <div className="flex items-center justify-between w-full overflow-hidden z-10 shrink-0">
                            <span className="text-[10px] sm:text-[11px] font-medium text-white/90 truncate tracking-wide">
                              {r.process.name}
                            </span>
                            {isGrouped && r.process.instanceCount > 1 && (
                              <span className="px-1.5 py-0.2 rounded text-[9px] font-medium bg-white/20 text-white/90 shrink-0">
                                ×{r.process.instanceCount}
                              </span>
                            )}
                          </div>
                        )}

                        {/* CENTER: Prominent Official App Logo in the center of the bubble */}
                        <div className="flex-1 flex items-center justify-center py-0.5 overflow-hidden z-0">
                          <div className="transition-transform duration-300 group-hover:scale-110 drop-shadow-md">
                            {getAppLogo(
                              r.process.name,
                              r.width > 120 && r.height > 100
                                ? "w-11 h-11 sm:w-14 sm:h-14"
                                : r.width > 70 && r.height > 70
                                ? "w-8 h-8 sm:w-9 sm:h-9"
                                : r.width > 40 && r.height > 40
                                ? "w-5 h-5 sm:w-6 sm:h-6"
                                : "w-4 h-4"
                            )}
                          </div>
                        </div>

                        {/* Box Bottom Bar: RAM Metric & Share (only when height permits) */}
                        {r.height >= 70 && r.width >= 60 && (
                          <div className="w-full flex items-baseline justify-between pt-1 border-t border-white/10 z-10 shrink-0">
                            <span className="text-[10px] sm:text-xs font-semibold text-white tracking-tight truncate">
                              {formatBytes(r.process.workingSetBytes)}
                            </span>
                            <span className="text-[9px] text-white/70 font-light italic shrink-0 ml-1">
                              {r.percentage.toFixed(1)}%
                            </span>
                          </div>
                        )}
                      </motion.div>
                    );
                  })
                ) : (
                  /* Empty Search State */
                  <div className="h-full flex flex-col items-center justify-center text-center p-6">
                    <AlertTriangle className="w-8 h-8 text-amber-400 mb-2" />
                    <p className="font-light italic text-white text-base">No matching processes found</p>
                    <p className="text-xs text-stone-400 mt-1 max-w-sm font-light">
                      No active process matched &quot;{searchTerm}&quot;.
                    </p>
                    <button
                      onClick={() => setSearchTerm('')}
                      className="mt-3 px-3 py-1.5 rounded-lg bg-white/10 hover:bg-white/15 text-xs text-white font-light italic"
                    >
                      Clear Search Filter
                    </button>
                  </div>
                )
              ) : (
                /* Compact Strip Mode */
                <div className="h-full flex items-center gap-2.5 px-3 overflow-x-auto">
                  {processes.slice(0, 12).map(p => (
                    <motion.button
                      key={p.id}
                      layout
                      onClick={() => setSelectedProcess(p)}
                      className="shrink-0 h-12 px-3 rounded-lg bg-white/[0.12] hover:bg-white/[0.22] border border-white/20 flex items-center gap-2.5 text-xs font-normal text-white transition active:scale-95"
                    >
                      <div className="w-5 h-5 shrink-0">
                        {getAppLogo(p.name, "w-5 h-5")}
                      </div>
                      <span className="font-light">{p.name}</span>
                      <span className="text-white font-semibold">{formatBytes(p.workingSetBytes)}</span>
                    </motion.button>
                  ))}
                </div>
              )}
            </AnimatePresence>

            {/* Process Inspection Modal */}
            <AnimatePresence>
              {selectedProcess && (
                <motion.div
                  initial={{ opacity: 0, scale: 0.95 }}
                  animate={{ opacity: 1, scale: 1 }}
                  exit={{ opacity: 0, scale: 0.95 }}
                  role="dialog"
                  aria-modal="true"
                  className="absolute inset-0 bg-black/75 backdrop-blur-md flex items-center justify-center p-4 z-50"
                  onClick={() => {
                    setSelectedProcess(null);
                    setIsTerminatingRestricted(false);
                  }}
                >
                  <div
                    className="w-full max-w-sm rounded-2xl bg-[#141416] border border-white/20 p-5 shadow-2xl text-left"
                    onClick={e => e.stopPropagation()}
                  >
                    <div className="flex items-center justify-between pb-3 border-b border-white/10">
                      <div className="flex items-center gap-3">
                        <div className="w-9 h-9 p-1 rounded-xl bg-white/5 border border-white/10 flex items-center justify-center">
                          {getAppLogo(selectedProcess.name, "w-7 h-7")}
                        </div>
                        <div>
                          <h3 className="font-light italic text-white text-base">{selectedProcess.name}</h3>
                          <p className="text-[11px] text-stone-400 font-mono">PID: {selectedProcess.id}</p>
                        </div>
                      </div>
                      <button
                        onClick={() => {
                          setSelectedProcess(null);
                          setIsTerminatingRestricted(false);
                        }}
                        aria-label="Close dialog"
                        className="p-1 rounded-lg hover:bg-white/10 text-stone-400 hover:text-white transition"
                      >
                        <X className="w-4 h-4" />
                      </button>
                    </div>

                    <div className="py-4 space-y-2.5 text-xs">
                      <div className="flex justify-between">
                        <span className="text-stone-400 font-light italic">Working Set (Physical RAM):</span>
                        <span className="font-semibold text-white">{formatBytes(selectedProcess.workingSetBytes)}</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-stone-400 font-light italic">Tracked RAM Share:</span>
                        <span className="font-light text-stone-200">
                          {((selectedProcess.workingSetBytes / usedPhysicalRAM) * 100).toFixed(1)}%
                        </span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-stone-400 font-light italic">Aggregated Instances:</span>
                        <span className="font-light text-stone-200">{selectedProcess.instanceCount}</span>
                      </div>
                      <div className="flex flex-col gap-1 pt-1">
                        <span className="text-stone-400 font-light italic">Executable Location:</span>
                        <code className="text-[10px] bg-black/60 p-1.5 rounded border border-white/10 text-stone-300 break-all font-mono">
                          {selectedProcess.path || 'System Kernel Module'}
                        </code>
                      </div>

                      {isTerminatingRestricted && (
                        <div className="p-2.5 rounded-lg bg-amber-950/40 border border-amber-500/40 text-amber-200 text-[11px] flex items-start gap-2 mt-2">
                          <AlertTriangle className="w-4 h-4 text-amber-400 shrink-0 mt-0.5" />
                          <span className="font-light italic">
                            Protected Process: Terminating critical OS services ({selectedProcess.name}) is restricted.
                          </span>
                        </div>
                      )}
                    </div>

                    <div className="flex items-center justify-end gap-2 pt-3 border-t border-white/10">
                      <button
                        onClick={() => {
                          setSelectedProcess(null);
                          setIsTerminatingRestricted(false);
                        }}
                        className="px-3.5 py-1.5 rounded-lg bg-white/5 hover:bg-white/10 border border-white/10 text-xs font-light text-stone-300"
                      >
                        Close
                      </button>
                      <button
                        onClick={async () => {
                          if (selectedProcess.category === 'system') {
                            setIsTerminatingRestricted(true);
                          } else if (isLiveMachineConnected) {
                            // Real termination via LocalApiServer
                            try {
                              const resp = await fetch('http://127.0.0.1:51888/api/kill', {
                                method: 'POST',
                                headers: { 'Content-Type': 'application/json' },
                                body: JSON.stringify({ pid: selectedProcess.id }),
                              });
                              const data = await resp.json();
                              if (data.success) {
                                showToast(`✅ Terminated ${selectedProcess.name} (PID ${selectedProcess.id})`);
                                setSelectedProcess(null);
                                setIsTerminatingRestricted(false);
                              } else {
                                showToast(`❌ ${data.error || 'Failed to terminate process'}`);
                              }
                            } catch {
                              showToast('❌ Could not reach RAMView agent. Is the desktop app running?');
                            }
                          } else {
                            // Simulated termination
                            setProcesses(prev => prev.filter(p => p.id !== selectedProcess.id));
                            showToast(`Simulated termination of ${selectedProcess.name}`);
                            setSelectedProcess(null);
                            setIsTerminatingRestricted(false);
                          }
                        }}
                        className="px-3.5 py-1.5 rounded-lg bg-stone-800 hover:bg-stone-700 text-stone-100 text-xs font-medium border border-stone-600 transition"
                      >
                        End Process
                      </button>
                    </div>
                  </div>
                </motion.div>
              )}
            </AnimatePresence>
          </div>

          {/* Bottom Filter Chips & Toggles */}
          <div className="flex flex-wrap items-center justify-between gap-3 pt-2 text-xs">
            {/* Filter Chips */}
            <div className="flex items-center gap-1.5 flex-wrap">
              {(['all', 'app', 'system', 'highest'] as const).map(preset => (
                <button
                  key={preset}
                  onClick={() => {
                    setFilterPreset(preset);
                    showToast(`Filtered by: ${preset === 'highest' ? 'Highest RAM' : preset.toUpperCase()}`);
                  }}
                  className={`px-3.5 py-1 rounded-full text-xs font-light italic capitalize transition active:scale-95 ${
                    filterPreset === preset
                      ? 'bg-white text-stone-900 font-medium shadow-sm'
                      : 'bg-white/5 hover:bg-white/10 text-stone-400 hover:text-stone-200 border border-white/10'
                  }`}
                >
                  {preset === 'highest' ? 'Highest RAM' : preset}
                </button>
              ))}
            </div>

            {/* Grouping Toggle */}
            <div className="flex items-center gap-2">
              <button
                onClick={() => {
                  setIsGrouped(g => !g);
                  showToast(isGrouped ? 'Grouping disabled' : 'Process grouping enabled');
                }}
                className={`flex items-center gap-1.5 px-3 py-1 rounded-lg border text-xs font-light italic transition active:scale-95 ${
                  isGrouped
                    ? 'bg-white/15 border-white/30 text-white'
                    : 'bg-white/5 border-white/10 text-stone-400'
                }`}
              >
                <Layers className="w-3.5 h-3.5" />
                <span>Grouped</span>
              </button>

              <span className="text-[11px] text-stone-500 font-light italic hidden sm:inline">
                {treemapRects.length} processes rendered
              </span>
            </div>
          </div>
        </motion.div>

        {/* Architectural Features Section (Cream Editorial Styling) */}
        <section id="features" className="mt-16 scroll-mt-20">
          <div className="text-center mb-8">
            <h2 className="text-2xl sm:text-3xl font-light italic text-[#1C1917] tracking-tight">
              Engineered for Windows Desktop Precision
            </h2>
            <p className="text-xs sm:text-sm text-[#78716C] font-light italic mt-1.5 max-w-xl mx-auto">
              Modern .NET 10 and Win32 interop architecture with zero background telemetry.
            </p>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
            <div className="p-6 rounded-2xl bg-white border border-[#E7E2D9] shadow-sm hover:shadow-md transition">
              <div className="w-9 h-9 rounded-xl bg-[#FAF7F2] border border-[#DDD7CD] flex items-center justify-center text-[#1C1917] mb-3.5">
                <HardDrive className="w-4 h-4" />
              </div>
              <h3 className="font-light italic text-[#1C1917] text-base">Working Set Precision</h3>
              <p className="text-xs text-[#78716C] font-light italic mt-2 leading-relaxed">
                Directly reads physical memory pages via kernel <code className="text-[#1C1917] font-mono bg-[#FAF7F2] px-1 py-0.5 rounded">GlobalMemoryStatusEx</code> and fault-tolerant process enumeration.
              </p>
            </div>

            <div className="p-6 rounded-2xl bg-white border border-[#E7E2D9] shadow-sm hover:shadow-md transition">
              <div className="w-9 h-9 rounded-xl bg-[#FAF7F2] border border-[#DDD7CD] flex items-center justify-center text-[#1C1917] mb-3.5">
                <Zap className="w-4 h-4" />
              </div>
              <h3 className="font-light italic text-[#1C1917] text-base">Squarified Geometry</h3>
              <p className="text-xs text-[#78716C] font-light italic mt-2 leading-relaxed">
                Bruls-Huizing-van Wijk algorithm computes optimal aspect ratios, ensuring logos and memory values remain balanced.
              </p>
            </div>

            <div className="p-6 rounded-2xl bg-white border border-[#E7E2D9] shadow-sm hover:shadow-md transition">
              <div className="w-9 h-9 rounded-xl bg-[#FAF7F2] border border-[#DDD7CD] flex items-center justify-center text-[#1C1917] mb-3.5">
                <ShieldCheck className="w-4 h-4" />
              </div>
              <h3 className="font-light italic text-[#1C1917] text-base">Shielded Process Safety</h3>
              <p className="text-xs text-[#78716C] font-light italic mt-2 leading-relaxed">
                Hardened OS denylist protects core system processes (<code className="text-[#1C1917] font-mono bg-[#FAF7F2] px-1 py-0.5 rounded">csrss</code>, <code className="text-[#1C1917] font-mono bg-[#FAF7F2] px-1 py-0.5 rounded">dwm</code>) from inadvertent termination.
              </p>
            </div>
          </div>
        </section>

        {/* Specifications Table (Cream Table) */}
        <section id="specs" className="mt-16 scroll-mt-20">
          <div className="p-6 sm:p-8 rounded-2xl bg-white border border-[#E7E2D9] shadow-sm">
            <h2 className="text-xl font-light italic text-[#1C1917] mb-4 flex items-center gap-2">
              <Info className="w-4 h-4 text-[#57534E]" />
              System Architecture &amp; Specifications
            </h2>
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs border-collapse font-light">
                <thead>
                  <tr className="border-b border-[#E7E2D9] text-[#78716C] font-normal italic">
                    <th className="py-2.5 px-3">Parameter</th>
                    <th className="py-2.5 px-3">Desktop Application</th>
                    <th className="py-2.5 px-3">Web Companion</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[#E7E2D9] text-[#292524]">
                  <tr>
                    <td className="py-2.5 px-3 font-normal text-[#1C1917]">Target Framework</td>
                    <td className="py-2.5 px-3 font-mono text-[#1C1917]">.NET 10 Desktop (WPF + Win32)</td>
                    <td className="py-2.5 px-3 font-mono text-[#57534E]">React 19 + Framer Motion</td>
                  </tr>
                  <tr>
                    <td className="py-2.5 px-3 font-normal text-[#1C1917]">Memory Metric</td>
                    <td className="py-2.5 px-3">Physical RAM (Working Set 64-bit)</td>
                    <td className="py-2.5 px-3">Real-time Stream / Emulation</td>
                  </tr>
                  <tr>
                    <td className="py-2.5 px-3 font-normal text-[#1C1917]">Window Capabilities</td>
                    <td className="py-2.5 px-3">Click-through Ghost HUD, Always on Top</td>
                    <td className="py-2.5 px-3">Fluid Responsive Canvas</td>
                  </tr>
                  <tr>
                    <td className="py-2.5 px-3 font-normal text-[#1C1917]">Global Hotkey</td>
                    <td className="py-2.5 px-3 font-mono text-emerald-700">Ctrl + Shift + R</td>
                    <td className="py-2.5 px-3 text-[#A8A29E]">—</td>
                  </tr>
                  <tr>
                    <td className="py-2.5 px-3 font-normal text-[#1C1917]">Data Privacy</td>
                    <td className="py-2.5 px-3 text-emerald-700">100% Offline • Zero Telemetry</td>
                    <td className="py-2.5 px-3 text-emerald-700">Local Only • No Tracking</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </section>

        {/* Windows Download & Install Hub (Cream Editorial Hero) */}
        <section id="download" className="mt-16 scroll-mt-20">
          <div className="rounded-2xl bg-[#EFECE6] border border-[#DDD7CD] p-6 sm:p-10 text-center relative overflow-hidden shadow-sm">
            <div className="max-w-xl mx-auto relative z-10">
              <h2 className="text-2xl sm:text-4xl font-light italic text-[#1C1917] tracking-tight">
                Download RAM VIEW for Windows
              </h2>
              <p className="text-xs sm:text-sm text-[#78716C] font-light italic mt-2.5 leading-relaxed">
                Get the unpackaged standalone executable or run the PowerShell automated setup.
              </p>

              {/* Action Buttons */}
              <div className="flex flex-wrap items-center justify-center gap-3 mt-6">
                <a
                  href="/downloads/Install-RAMView.ps1"
                  download
                  onClick={() => showToast('PowerShell installer script download started.')}
                  className="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-[#1C1917] hover:bg-[#292524] text-[#FAF7F2] text-xs sm:text-sm font-medium transition hover:scale-105 active:scale-95 shadow"
                >
                  <Download className="w-4 h-4" />
                  Install Script (.ps1)
                </a>

                <a
                  href="/downloads/RAMView.cmd"
                  download
                  onClick={() => showToast('Batch launcher script download started.')}
                  className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-white hover:bg-[#FAF7F2] border border-[#DDD7CD] text-[#1C1917] text-xs sm:text-sm font-light italic transition active:scale-95"
                >
                  <Terminal className="w-4 h-4 text-[#57534E]" />
                  Batch Launcher (.cmd)
                </a>
              </div>

              {/* Quick CLI copy block */}
              <div className="mt-6 p-3 rounded-xl bg-white border border-[#DDD7CD] max-w-md mx-auto flex items-center justify-between text-left shadow-sm">
                <code className="text-[11px] text-[#1C1917] font-mono truncate mr-2">
                  .\installer\Install-RAMView.ps1
                </code>
                <button
                  onClick={copyInstallCommand}
                  aria-label="Copy installation command"
                  className="p-1.5 rounded-lg bg-[#FAF7F2] hover:bg-[#EFECE6] border border-[#DDD7CD] text-[#1C1917] transition active:scale-90"
                  title="Copy command"
                >
                  <Copy className="w-3.5 h-3.5" />
                </button>
              </div>

              <p className="text-[11px] text-[#A8A29E] font-light italic mt-4">
                Compatible with Windows 10 (1809+) &amp; Windows 11 64-bit.
              </p>
            </div>
          </div>
        </section>

        {/* Support & Contact Section */}
        <section id="contact" className="mt-16 scroll-mt-20 border-t border-[#E7E2D9] pt-10">
          <div className="max-w-2xl mx-auto text-center space-y-2">
            <h2 className="text-base font-light italic text-[#1C1917]">Community &amp; Developer Support</h2>
            <p className="text-xs text-[#78716C] font-light italic leading-relaxed">
              Have feedback, questions, or architectural suggestions?
            </p>
            <div className="flex flex-wrap items-center justify-center gap-4 pt-2 text-xs font-light italic">
              <a
                href="mailto:support@ramview.local?subject=RAM%20VIEW%20Support"
                className="flex items-center gap-1.5 text-[#1C1917] hover:underline"
              >
                <Mail className="w-3.5 h-3.5 text-[#57534E]" />
                support@ramview.local
              </a>
              <span className="text-[#DDD7CD]">•</span>
              <a
                href="tel:+18005550199"
                className="flex items-center gap-1.5 text-[#57534E] hover:text-[#1C1917]"
              >
                <Phone className="w-3.5 h-3.5" />
                +1 (800) 555-0199
              </a>
            </div>
          </div>
        </section>
      </main>

      {/* Footer (Cream Styling) */}
      <footer className="border-t border-[#E7E2D9] bg-[#F3EFE8] py-8 px-4 sm:px-8 text-xs text-[#78716C] mt-16">
        <div className="max-w-6xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-4 font-light italic">
          <div className="flex items-center gap-2">
            <Cpu className="w-4 h-4 text-[#1C1917]" />
            <span className="font-medium text-[#1C1917] not-italic">RAM VIEW</span>
            <span>• &copy; {new Date().getFullYear()} RAM View Team. Open-Source.</span>
          </div>

          <div className="flex items-center gap-5 text-xs">
            <button
              onClick={() => setModalType('requirements')}
              className="hover:text-[#1C1917] transition"
            >
              Requirements
            </button>
            <button
              onClick={() => setModalType('privacy')}
              className="hover:text-[#1C1917] transition"
            >
              Privacy
            </button>
            <button
              onClick={() => setModalType('changelog')}
              className="hover:text-[#1C1917] transition"
            >
              Changelog
            </button>
            <a
              href="https://github.com"
              target="_blank"
              rel="noreferrer"
              className="hover:text-[#1C1917] flex items-center gap-1 transition"
            >
              GitHub <ExternalLink className="w-3 h-3" />
            </a>
          </div>
        </div>
      </footer>

      {/* Info Modals */}
      <AnimatePresence>
        {modalType && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            role="dialog"
            aria-modal="true"
            className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4"
            onClick={() => setModalType(null)}
          >
            <div
              className="w-full max-w-md rounded-2xl bg-white border border-[#E7E2D9] p-6 shadow-2xl text-left"
              onClick={e => e.stopPropagation()}
            >
              <div className="flex items-center justify-between pb-3 border-b border-[#E7E2D9]">
                <h3 className="font-light italic text-[#1C1917] text-base">
                  {modalType === 'requirements' && 'System Requirements'}
                  {modalType === 'privacy' && 'Privacy & Data Security'}
                  {modalType === 'changelog' && 'Version 1.0.0 Release Notes'}
                </h3>
                <button
                  onClick={() => setModalType(null)}
                  className="p-1 rounded-lg hover:bg-[#FAF7F2] text-[#78716C] hover:text-[#1C1917]"
                >
                  <X className="w-4 h-4" />
                </button>
              </div>

              <div className="py-4 text-xs text-[#57534E] space-y-2.5 leading-relaxed font-light italic">
                {modalType === 'requirements' && (
                  <>
                    <p>• Operating System: Windows 10 (Version 1809+) or Windows 11 (64-bit).</p>
                    <p>• Runtime: .NET 10 Windows Desktop Runtime.</p>
                    <p>• Memory Footprint: ~15 MB during active monitoring.</p>
                    <p>• Display: High-DPI PerMonitorV2 scaling support.</p>
                  </>
                )}

                {modalType === 'privacy' && (
                  <>
                    <p>• 100% Offline: RAM VIEW performs zero network telemetry or external reporting.</p>
                    <p>• Local In-Memory Queries: Evaluated strictly in-memory on your system.</p>
                    <p>• Loopback REST Server: Listens strictly on 127.0.0.1 for local companion tools.</p>
                  </>
                )}

                {modalType === 'changelog' && (
                  <>
                    <p>• Initial Release: Squarified treemap engine with 20 MB floor and anti-dominance capping.</p>
                    <p>• Click-Through HUD: Win32 extended transparent layering with Ctrl+Shift+R toggle.</p>
                    <p>• Centered App Logos: High-resolution brand icons rendered in the center of each RAM bubble.</p>
                  </>
                )}
              </div>

              <div className="pt-3 border-t border-[#E7E2D9] text-right">
                <button
                  onClick={() => setModalType(null)}
                  className="px-4 py-1.5 rounded-lg bg-[#1C1917] text-xs font-normal text-white"
                >
                  Close
                </button>
              </div>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}

// Helpers
function isSystemProc(name: string): boolean {
  const sys = ['system', 'idle', 'registry', 'csrss', 'dwm', 'smss', 'wininit', 'services', 'lsass', 'svchost', 'explorer'];
  return sys.some(s => name.toLowerCase().includes(s));
}

export default App;
