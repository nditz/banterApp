"use client";

import type { ReactNode } from "react";
import { motion, useReducedMotion } from "framer-motion";
import { Target, Trophy, Zap } from "lucide-react";
import type { WelcomeSlideData } from "@/components/home/WelcomeSlideBody";
import { motionEase } from "@/lib/motionConfig";

interface WelcomeHeroSlideProps {
  slide: WelcomeSlideData;
  eyebrow?: ReactNode;
  footer?: ReactNode;
  active?: boolean;
}

const stepIcons = [Target, Zap, Trophy] as const;

export function WelcomeHeroSlide({
  slide,
  eyebrow,
  footer,
  active = true,
}: WelcomeHeroSlideProps) {
  const reduceMotion = useReducedMotion();

  return (
    <motion.div
      className="welcome-hero"
      initial={false}
      animate={
        reduceMotion || active ? { opacity: 1, y: 0 } : { opacity: 0.55, y: 10 }
      }
      transition={{ duration: 0.5, ease: motionEase }}
    >
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
