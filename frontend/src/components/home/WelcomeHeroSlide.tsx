"use client";

import type { ReactNode } from "react";
import Image from "next/image";
import { Target, Trophy, Zap } from "lucide-react";
import type { WelcomeSlideData } from "@/components/home/WelcomeSlideBody";
import { cn } from "@/lib/utils";

interface WelcomeHeroSlideProps {
  slide: WelcomeSlideData;
  eyebrow?: ReactNode;
  footer?: ReactNode;
}

const stepIcons = [Target, Zap, Trophy] as const;

export function WelcomeHeroSlide({ slide, eyebrow, footer }: WelcomeHeroSlideProps) {
  return (
    <div className="welcome-hero">
      <div className="welcome-hero__copy">
        <span className="welcome-hero__chip">{slide.subtitle}</span>

        {eyebrow}

        <h2 className="welcome-hero__title">{slide.title}</h2>
        <p className="welcome-hero__body">{slide.body}</p>

        {slide.highlights && slide.highlights.length > 0 && (
          <ul className="welcome-hero__highlights" aria-label="Highlights">
            {slide.highlights.map((item, i) => {
              const Icon = stepIcons[i % stepIcons.length];
              return (
                <li key={item} className="welcome-hero__highlight">
                  <Icon className="size-3 shrink-0 text-brand" aria-hidden />
                  {item}
                </li>
              );
            })}
          </ul>
        )}

        {footer}
      </div>

      <div className="welcome-hero__visual" aria-hidden>
        <div className={cn("welcome-hero__frame", `welcome-hero__frame--${slide.accent}`)}>
          <Image
            src={slide.backgroundImage}
            alt=""
            width={400}
            height={500}
            className="welcome-hero__image"
            priority
          />
          <div className="welcome-hero__shine" />
          {slide.stickerImage && (
            <Image
              src={slide.stickerImage}
              alt=""
              width={52}
              height={52}
              className="welcome-hero__sticker"
            />
          )}
        </div>
      </div>
    </div>
  );
}
