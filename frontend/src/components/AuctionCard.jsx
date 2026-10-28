import React from 'react'
import { Card, CardContent, CardFooter } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { CountdownTimer } from './CountdownTimer'
import { Gavel, Zap } from 'lucide-react'
import { resolveImageUrl } from '@/lib/utils'

export function AuctionCard({ auction, onSelect }) {
  const isCompleted = auction.status === 2 || new Date(auction.endTime) <= new Date()
  const fallbackImage = 'https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=600&auto=format&fit=crop&q=80'

  return (
    <Card 
      onClick={() => onSelect(auction.id)}
      className="group overflow-hidden flex flex-col justify-between border-slate-200 hover:border-blue-400 hover:shadow-md transition-all cursor-pointer bg-white"
    >
      <div>
        {/* Image Thumbnail with status tags */}
        <div className="relative aspect-[4/3] w-full overflow-hidden bg-slate-100">
          <img
            src={resolveImageUrl(auction.imageUrl) || fallbackImage}
            alt={auction.title}
            className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
            onError={(e) => { e.target.src = fallbackImage }}
          />

          <div className="absolute top-2.5 left-2.5 flex flex-col gap-1">
            {auction.categoryName && (
              <Badge variant="secondary" className="backdrop-blur-sm bg-white/90 text-slate-800 text-[10px] font-bold">
                {auction.categoryName}
              </Badge>
            )}
            {auction.buyItNowPrice && !isCompleted && (
              <Badge variant="success" className="text-[10px] flex items-center gap-0.5">
                <Zap className="w-2.5 h-2.5 fill-current" /> Buy Now Available
              </Badge>
            )}
          </div>

          <div className="absolute bottom-2.5 right-2.5">
            <CountdownTimer endTime={auction.endTime} />
          </div>
        </div>

        {/* Content */}
        <CardContent className="p-4 space-y-2">
          <h3 className="font-bold text-base text-slate-900 group-hover:text-blue-600 transition-colors line-clamp-1">
            {auction.title}
          </h3>
          <p className="text-xs text-slate-500 line-clamp-2">
            {auction.description}
          </p>
        </CardContent>
      </div>

      {/* Pricing & Footer Action */}
      <CardFooter className="p-4 pt-0 flex items-center justify-between border-t border-slate-100 mt-2">
        <div>
          <span className="text-[10px] uppercase font-bold text-slate-400 tracking-wider block">
            {isCompleted ? 'Final Sold Price' : 'Current Bid'}
          </span>
          <span className="text-xl font-extrabold text-blue-600">
            ${auction.currentPrice?.toFixed(2)}
          </span>
        </div>

        <Button size="sm" variant={isCompleted ? "outline" : "default"} className="font-semibold text-xs gap-1.5">
          <Gavel className="w-3.5 h-3.5" />
          {isCompleted ? 'View Result' : 'Bid Now'}
        </Button>
      </CardFooter>
    </Card>
  )
}
