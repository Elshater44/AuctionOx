import React, { useState, useEffect } from 'react'
import { Clock } from 'lucide-react'

export function CountdownTimer({ endTime, onExpire }) {
  const [timeLeft, setTimeLeft] = useState(() => calculateTimeLeft(endTime))

  function calculateTimeLeft(targetDate) {
    const totalMs = new Date(targetDate).getTime() - new Date().getTime()
    if (totalMs <= 0) {
      return { totalMs: 0, days: 0, hours: 0, minutes: 0, seconds: 0, isEnded: true }
    }

    const seconds = Math.floor((totalMs / 1000) % 60)
    const minutes = Math.floor((totalMs / 1000 / 60) % 60)
    const hours = Math.floor((totalMs / (1000 * 60 * 60)) % 24)
    const days = Math.floor(totalMs / (1000 * 60 * 60 * 24))

    return { totalMs, days, hours, minutes, seconds, isEnded: false }
  }

  useEffect(() => {
    const timer = setInterval(() => {
      const remaining = calculateTimeLeft(endTime)
      setTimeLeft(remaining)

      if (remaining.isEnded) {
        clearInterval(timer)
        if (onExpire) onExpire()
      }
    }, 1000)

    return () => clearInterval(timer)
  }, [endTime, onExpire])

  if (timeLeft.isEnded) {
    return (
      <span className="inline-flex items-center gap-1 text-xs font-semibold px-2 py-0.5 rounded bg-slate-100 text-slate-500 border border-slate-200">
        <Clock className="w-3 h-3" /> Ended
      </span>
    )
  }

  // Under 5 minutes = Hot urgent warning (Red)
  const isUrgent = timeLeft.totalMs < 5 * 60 * 1000
  // Under 1 hour = Amber warning
  const isWarning = timeLeft.totalMs < 60 * 60 * 1000

  const badgeStyle = isUrgent
    ? 'bg-red-50 text-red-700 border-red-200 animate-pulse'
    : isWarning
    ? 'bg-amber-50 text-amber-700 border-amber-200'
    : 'bg-slate-50 text-slate-700 border-slate-200'

  const pad = (n) => String(n).padStart(2, '0')

  return (
    <span className={`inline-flex items-center gap-1.5 text-xs font-mono font-semibold px-2.5 py-0.5 rounded border ${badgeStyle}`}>
      <Clock className="w-3 h-3 shrink-0" />
      {timeLeft.days > 0 && `${timeLeft.days}d `}
      {pad(timeLeft.hours)}h {pad(timeLeft.minutes)}m {pad(timeLeft.seconds)}s
    </span>
  )
}
