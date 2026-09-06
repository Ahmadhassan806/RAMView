import React from 'react';

// Crisp, scalable official SVG brand logos for major desktop processes
export function getAppLogo(processName: string, className = "w-7 h-7"): React.ReactNode {
  const name = processName.toLowerCase();

  // Google / Chrome
  if (name.includes('chrome') || name.includes('google')) {
    return (
      <svg className={className} viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
        <circle cx="24" cy="24" r="20" fill="#EA4335" />
        <path d="M24 13.5L34.8 32.2H13.2L24 13.5Z" fill="white" fillOpacity="0.2"/>
        <circle cx="24" cy="24" r="9.5" fill="#FFFFFF" />
        <circle cx="24" cy="24" r="7.5" fill="#4285F4" />
        <path d="M24 4C30.4 4 36.1 7.1 39.7 12L28.5 24H14.5L24 4Z" fill="#EA4335" />
        <path d="M43.6 20C44.4 22 44.4 24.3 44 26.5L32.8 26.5L27 16.5L39.7 12C41.5 14.2 42.8 17 43.6 20Z" fill="#FBBC05" />
        <path d="M14.5 24L20.2 34H8.4C5.7 30 4.2 25.2 4.2 20C4.2 16.8 5 13.8 6.5 11.2L16.2 28L14.5 24Z" fill="#34A853" />
        <path d="M24 44C17.3 44 11.4 40.7 7.8 35.5L19 16L24.8 26L19 36H36.3C33 41 27.2 44 24 44Z" fill="#34A853" fillOpacity="0.9" />
        <path d="M32.8 26.5C32.8 31.4 28.9 35.3 24 35.3C21.7 35.3 19.6 34.4 18 33L24 24H43.9C43.9 24.8 43.8 25.7 43.6 26.5H32.8Z" fill="#FBBC05" />
      </svg>
    );
  }

  // Claude / Anthropic
  if (name.includes('claude')) {
    return (
      <svg className={className} viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect width="24" height="24" rx="6" fill="#D97706" />
        <path d="M12 4L13.5 9.5L19 11L13.5 12.5L12 18L10.5 12.5L5 11L10.5 9.5L12 4Z" fill="#FFFFFF" />
      </svg>
    );
  }

  // Microsoft Edge / WebView2
  if (name.includes('edge') || name.includes('msedge')) {
    return (
      <svg className={className} viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
        <circle cx="24" cy="24" r="20" fill="#0C59A4" />
        <path d="M24 8C32.8 8 40 15.2 40 24C40 32.8 32.8 40 24 40C15.2 40 8 32.8 8 24C8 17.5 11.8 11.9 17.3 9.3C18.6 15.1 23.2 19.5 29 20C30.6 20.1 32 19.7 33.3 19C33.7 20.6 34 22.3 34 24C34 29.5 29.5 34 24 34C19 34 14.8 30.3 14.1 25.5C14 24.8 14.3 24 15 23.6C15.7 23.2 16.6 23.4 17 24.1C17.6 25.8 19.2 27 21.1 27C23.6 27 25.6 25 25.6 22.5C25.6 20.9 24.7 19.5 23.3 18.8C18.1 16.2 14 11.3 14 5.5C17 7.1 20.4 8 24 8Z" fill="#50E6FF" />
      </svg>
    );
  }

  // Antigravity
  if (name.includes('antigravity')) {
    return (
      <svg className={className} viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect width="24" height="24" rx="6" fill="#1E293B" stroke="#64748B" strokeWidth="1.5" />
        <path d="M12 4L16.5 12H7.5L12 4Z" fill="#F59E0B" />
        <circle cx="12" cy="16" r="2.5" fill="#38BDF8" />
      </svg>
    );
  }

  // VS Code / Code
  if (name.includes('code') || name.includes('devenv')) {
    return (
      <svg className={className} viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M36.2 5.1L24.8 15.7L14.7 8L9.2 10.6L18.4 24L9.2 37.4L14.7 40L24.8 32.3L36.2 42.9C37.8 44.4 40.5 43.6 41 41.4L44.8 8.6C45.2 6.4 42.8 4.7 40.8 5.6L36.2 5.1Z" fill="#007ACC" />
        <path d="M36.2 5.1L24.8 15.7L18.4 24L24.8 32.3L36.2 42.9L42.5 40.5L42.5 7.5L36.2 5.1Z" fill="#1F9CF0" />
        <path d="M36.2 16.5L27 24L36.2 31.5L41 28.5L41 19.5L36.2 16.5Z" fill="#FFFFFF" fillOpacity="0.8" />
      </svg>
    );
  }

  // Discord
  if (name.includes('discord')) {
    return (
      <svg className={className} viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect width="48" height="48" rx="12" fill="#5865F2" />
        <path d="M34.5 15.8C32.4 14.8 30.1 14.1 27.7 13.7C27.4 14.3 27.1 15 26.8 15.6C24.2 15.2 21.7 15.2 19.2 15.6C18.9 15 18.6 14.3 18.3 13.7C15.9 14.1 13.6 14.8 11.5 15.8C7.4 22 6.3 28 6.9 33.9C9.6 35.9 12.3 37.2 14.9 38C15.5 37.1 16.1 36.2 16.6 35.2C15.7 34.8 14.8 34.4 14 33.8C14.2 33.6 14.4 33.5 14.6 33.3C19.7 35.7 25.3 35.7 30.4 33.3C30.6 33.5 30.8 33.6 31 33.8C30.2 34.4 29.3 34.8 28.4 35.2C28.9 36.2 29.5 37.1 30.1 38C32.7 37.2 35.4 35.9 38.1 33.9C38.8 27 37 21 34.5 15.8ZM18.2 29.2C16.6 29.2 15.3 27.7 15.3 25.9C15.3 24.1 16.6 22.6 18.2 22.6C19.8 22.6 21.1 24.1 21.1 25.9C21.1 27.7 19.8 29.2 18.2 29.2ZM29.8 29.2C28.2 29.2 26.9 27.7 26.9 25.9C26.9 24.1 28.2 22.6 29.8 22.6C31.4 22.6 32.7 24.1 32.7 25.9C32.7 27.7 31.4 29.2 29.8 29.2Z" fill="white"/>
      </svg>
    );
  }

  // Spotify
  if (name.includes('spotify')) {
    return (
      <svg className={className} viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
        <circle cx="24" cy="24" r="20" fill="#1DB954" />
        <path d="M32.5 29.6C32.1 30.2 31.3 30.4 30.7 30C25.4 26.8 18.8 26.1 11 27.9C10.3 28.1 9.6 27.6 9.4 26.9C9.2 26.2 9.7 25.5 10.4 25.3C18.9 23.4 26.2 24.2 32.1 27.8C32.7 28.1 32.9 29 32.5 29.6ZM34.9 24C34.3 24.8 33.2 25.1 32.4 24.6C26.5 21 17.5 19.9 10.5 22C9.6 22.3 8.6 21.7 8.3 20.8C8 19.9 8.6 18.9 9.5 18.6C17.5 16.2 27.5 17.4 34.3 21.5C35.1 22 35.4 23.2 34.9 24ZM35.2 18.2C28.1 14 16.3 13.6 9.5 15.7C8.4 16 7.2 15.4 6.9 14.3C6.6 13.2 7.2 12 8.3 11.7C16.1 9.3 29.1 9.8 37.3 14.7C38.3 15.3 38.6 16.6 38 17.6C37.4 18.6 36.2 18.8 35.2 18.2Z" fill="white"/>
      </svg>
    );
  }

  // Windows / Explorer / DWM
  if (name.includes('explorer') || name.includes('dwm') || name.includes('sihost') || name.includes('shellexperience')) {
    return (
      <svg className={className} viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M6 7.5L20.5 5.5V22H6V7.5Z" fill="#0078D7" />
        <path d="M23 5.2L42 2.5V22H23V5.2Z" fill="#0078D7" />
        <path d="M6 24.5H20.5V41L6 39V24.5Z" fill="#0078D7" />
        <path d="M23 24.5H42V44L23 41.3V24.5Z" fill="#0078D7" />
      </svg>
    );
  }

  // Node.js
  if (name.includes('node')) {
    return (
      <svg className={className} viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
        <polygon points="24,4 42,14.4 42,35.2 24,45.6 6,35.2 6,14.4" fill="#339933" />
        <polygon points="24,8 38,16 38,32 24,40 10,32 10,16" fill="#026E00" />
        <path d="M24 16V32M16 20L24 24L32 20M16 28L24 32L32 28" stroke="#FFFFFF" strokeWidth="2.5" strokeLinecap="round"/>
      </svg>
    );
  }

  // .NET / dotnet
  if (name.includes('dotnet')) {
    return (
      <svg className={className} viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
        <circle cx="24" cy="24" r="20" fill="#512BD4" />
        <text x="24" y="29" fill="white" fontSize="13" fontWeight="bold" fontFamily="sans-serif" textAnchor="middle">.NET</text>
      </svg>
    );
  }

  // Slack
  if (name.includes('slack')) {
    return (
      <svg className={className} viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect width="48" height="48" rx="10" fill="#4A154B" />
        <path d="M16 22a3 3 0 1 1 0-6 3 3 0 0 1 0 6zm0 2a3 3 0 0 1 3 3v6a3 3 0 1 1-6 0v-6a3 3 0 0 1 3-3zm10-8a3 3 0 1 1 6 0 3 3 0 0 1-6 0zm-2 0a3 3 0 0 1 3-3h6a3 3 0 1 1 0 6h-6a3 3 0 0 1-3-3zm8 16a3 3 0 1 1 0 6 3 3 0 0 1 0-6zm0-2a3 3 0 0 1-3-3v-6a3 3 0 1 1 6 0v6a3 3 0 0 1-3 3zm-10 8a3 3 0 1 1-6 0 3 3 0 0 1 6 0zm2 0a3 3 0 0 1-3 3h-6a3 3 0 1 1 0-6h6a3 3 0 0 1 3 3z" fill="#E01E5A"/>
      </svg>
    );
  }

  // Docker
  if (name.includes('docker')) {
    return (
      <svg className={className} viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect width="48" height="48" rx="10" fill="#0DB7ED" />
        <path d="M38 22c-.6-3.8-3.4-6-3.4-6-.4 1.4-1.2 2.3-2.1 2.8C30.2 16.9 26 17 26 17v-3h-4v3h-4v-3h-4v3h-4v-3H6v7c0 7 5 11 13 11 9 0 15-4.5 16.5-12.5.9-.1 2.1-.5 2.5-1.5z" fill="white"/>
        <rect x="10" y="11" width="3" height="2.5" fill="white"/>
        <rect x="14" y="11" width="3" height="2.5" fill="white"/>
        <rect x="18" y="11" width="3" height="2.5" fill="white"/>
        <rect x="14" y="7.5" width="3" height="2.5" fill="white"/>
        <rect x="18" y="7.5" width="3" height="2.5" fill="white"/>
      </svg>
    );
  }

  // Steam
  if (name.includes('steam')) {
    return (
      <svg className={className} viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
        <circle cx="24" cy="24" r="20" fill="#171A21" />
        <path d="M24 6C14.1 6 6 14.1 6 24C6 29.5 8.5 34.4 12.4 37.7L18.8 28.5C18.3 27.2 18 25.6 18 24C18 20.7 20.7 18 24 18C27.3 18 30 20.7 30 24C30 27.3 27.3 30 24 30C23.6 30 23.3 30 23 29.9L16.4 39.5C18.7 41.1 21.2 42 24 42C33.9 42 42 33.9 42 24C42 14.1 33.9 6 24 6Z" fill="#FFFFFF" fillOpacity="0.85"/>
      </svg>
    );
  }

  // Notion
  if (name.includes('notion')) {
    return (
      <svg className={className} viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect width="48" height="48" rx="10" fill="#000000" />
        <path d="M12 12l18-3v22l-18 3V12zm18-3l6 4v22l-6-3V9zM18 17v12l7-10v12" stroke="#FFFFFF" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"/>
      </svg>
    );
  }

  // Windows Terminal / PowerShell / CMD
  if (name.includes('terminal') || name.includes('powershell') || name.includes('cmd')) {
    return (
      <svg className={className} viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect width="48" height="48" rx="10" fill="#2D2D2D" />
        <path d="M12 16L20 24L12 32M22 32H34" stroke="#4ADE80" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round"/>
      </svg>
    );
  }

  // Security / svchost / services
  if (name.includes('svchost') || name.includes('services') || name.includes('system') || name.includes('lsass')) {
    return (
      <svg className={className} viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
        <circle cx="24" cy="24" r="20" fill="#334155" />
        <path d="M24 10L36 15V23C36 30.5 30.9 37.4 24 39C17.1 37.4 12 30.5 12 23V15L24 10Z" fill="#94A3B8" />
        <path d="M21 24L24 27L30 21" stroke="#0F172A" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"/>
      </svg>
    );
  }

  // Default fallback elegant memory chip icon
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
      <rect x="4" y="4" width="16" height="16" rx="4" fill="#44403C" stroke="#78716C" strokeWidth="1.5" />
      <circle cx="12" cy="12" r="3" fill="#D6D3D1" />
    </svg>
  );
}
