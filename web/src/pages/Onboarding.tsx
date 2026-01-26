import { Button, Typography } from 'antd';
import { useMemo, useState } from 'react';
import styles from './Onboarding.module.css';

import intro1 from '../assets/icons/intro1.svg';
import intro2 from '../assets/icons/intro2.svg';
import intro3 from '../assets/icons/intro3.svg';

const { Title, Text } = Typography;

type OnboardingPage = {
  image: string;
  title: string;
  desc: string;
  buttonText: string;
};

type Props = {
  onFinish: () => void;
};

const Onboarding = ({ onFinish }: Props) => {
  const pages: OnboardingPage[] = useMemo(
    () => [
      {
        image: intro1,
        title: 'Track Your Carbon Footprint',
        desc: 'Understand the environmental impact of your daily activities.',
        buttonText: 'Next',
      },
      {
        image: intro2,
        title: 'Set Emission Goals',
        desc: 'Set and track your personal goals to reduce emissions and contribute to the planet.',
        buttonText: 'Next',
      },
      {
        image: intro3,
        title: 'Build Green Habits',
        desc: 'Develop a sustainable lifestyle through small, impactful changes.',
        buttonText: 'Finish',
      },
    ],
    []
  );

  const [page, setPage] = useState(0);

  const handlePrimary = () => {
    if (page < pages.length - 1) {
      setPage((p) => p + 1);
      return;
    }
    onFinish();
  };

  return (
    <div className={styles.root}>
      <div className={styles.skip}>
        <Button type="text" className={styles.skipButton} onClick={onFinish}>
          Skip
        </Button>
      </div>

      <div className={styles.carousel}>
        <div
          className={styles.track}
          style={{ transform: `translateX(-${page * 100}%)` }}
        >
          {pages.map((p) => (
            <div key={p.title} className={styles.slide}>
              <div className={styles.imageWrap}>
                <img className={styles.image} src={p.image} alt={p.title} />
              </div>
              <Title level={2} className={styles.title}>
                {p.title}
              </Title>
              <Text className={styles.desc}>{p.desc}</Text>
            </div>
          ))}
        </div>
      </div>

      <div className={styles.footer}>
        <div className={styles.dots} aria-label="Onboarding progress">
          {pages.map((_, idx) => (
            <span
              key={idx}
              className={`${styles.dot} ${idx === page ? styles.dotActive : ''}`}
            />
          ))}
        </div>

        <Button
          type="primary"
          size="large"
          className={styles.primary}
          onClick={handlePrimary}
        >
          {pages[page].buttonText}
        </Button>
      </div>
    </div>
  );
};

export default Onboarding;

