"use client";

import type { ReactNode } from "react";
import { motion, useReducedMotion } from "framer-motion";
import { Clapperboard, Target, Trophy } from "lucide-react";
import type { WelcomeSlideData } from "@/components/home/WelcomeSlideBody";
import { motionEase } from "@/lib/motionConfig";

interface WelcomeHeroSlideProps {
  slide: WelcomeSlideData;
  eyebrow?: ReactNode;
  footer?: ReactNode;
  active?: boolean;
}

const stepIcons = [Target, Trophy, Clapperboard] as const;

export function WelcomeHeroSlide({
  slide,
  eyebrow,
  footer,
  active = true,
}: WelcomeHeroSlideProps) {
  const reduceMotion = useReducedMotion();

  return (
    <motion.div
      className="welcome-hero min-h-0"
      initial={false}
      animate={
        reduceMotion || active ? { opacity: 1, y: 0 } : { opacity: 0.55, y: 10 }
      }
      transition={{ duration: 0.45, ease: motionEase }}
    >
      <div className="welcome-hero__copy min-w-0">
        <span className="welcome-hero__chip">{slide.subtitle}</span>

        {eyebrow}

        <h1 className="welcome-hero__title text-balance">{slide.title}</h1>
        <p className="welcome-hero__body">{slide.body}</p>

        {slide.highlights && slide.highlights.length > 0 && (
          <ul className="welcome-hero__highlights" aria-label="What you can do">
            {slide.highlights.map((item, i) => {
              const Icon = stepIcons[i % stepIcons.length];
              return (
                <li key={item} className="welcome-hero__highlight">
                  <Icon className="size-3 shrink-0 text-muted-foreground" aria-hidden />
                  {item}
                </li>
              );
            })}
          </ul>
        )}

        {footer}
      </div>
    </motion.div>
  );
}
