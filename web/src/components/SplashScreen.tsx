import { Typography } from 'antd';
import styles from './SplashScreen.module.css';
import splashIcon from '../assets/icons/splash.svg';

const { Text } = Typography;

const SplashScreen = () => {
  return (
    <div className={styles.root} role="status" aria-label="Loading">
      <div className={styles.content}>
        <img className={styles.logo} src={splashIcon} alt="EcoLens logo" />
        <Text className={styles.title}>EcoLens</Text>
      </div>
    </div>
  );
};

export default SplashScreen;

