#import <UIKit/UIKit.h>
#import <AudioToolbox/AudioToolbox.h>

bool SupportsHapticFeedback() {
    if (@available(iOS 10.0, *)) {
        // 获取设备的型号
        NSString *deviceModel = [UIDevice currentDevice].model;

        // 如果是 iPhone，并且是 iPhone 7 及其更新的设备，支持 Haptic Feedback
        if ([deviceModel hasPrefix:@"iPhone"]) {
            if ([UIDevice currentDevice].systemVersion.floatValue >= 10.0) {
                return YES;
            }
        }
    }
    return NO;
}

void _TriggerHapticFeedback(int style) {
    if (@available(iOS 10.0, *)) {
        // 首先判断设备是否支持 haptic 反馈
        if (SupportsHapticFeedback()) {
            switch (style) {
                case 0: {
                    UIImpactFeedbackGenerator *lightFeedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
                    [lightFeedback prepare];
                    [lightFeedback impactOccurred];
                    break;
                }
                case 1: {
                    UIImpactFeedbackGenerator *mediumFeedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
                    [mediumFeedback prepare];
                    [mediumFeedback impactOccurred];
                    break;
                }
                case 2: {
                    UIImpactFeedbackGenerator *heavyFeedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
                    [heavyFeedback prepare];
                    [heavyFeedback impactOccurred];
                    break;
                }
                case 3: {
                    UISelectionFeedbackGenerator *selectionFeedback = [[UISelectionFeedbackGenerator alloc] init];
                    [selectionFeedback prepare];
                    [selectionFeedback selectionChanged];
                    break;
                }
                case 4: {
                    if (@available(iOS 13.0, *)) {
                        UINotificationFeedbackGenerator *notificationGenerator = [[UINotificationFeedbackGenerator alloc] init];
                        [notificationGenerator notificationOccurred:UINotificationFeedbackTypeSuccess];
                    }
                    break;
                }
                case 5: {
                    if (@available(iOS 13.0, *)) {
                        UINotificationFeedbackGenerator *notificationGenerator = [[UINotificationFeedbackGenerator alloc] init];
                        [notificationGenerator notificationOccurred:UINotificationFeedbackTypeWarning];
                    }
                    break;
                }
                case 6: {
                    if (@available(iOS 13.0, *)) {
                        UINotificationFeedbackGenerator *notificationGenerator = [[UINotificationFeedbackGenerator alloc] init];
                        [notificationGenerator notificationOccurred:UINotificationFeedbackTypeError];
                    }
                    break;
                }
                default: {
                    UIImpactFeedbackGenerator *defaultFeedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
                    [defaultFeedback prepare];
                    [defaultFeedback impactOccurred];
                    break;
                }
            }
        } else {
            // 如果设备不支持 haptic 反馈，使用传统的振动
            AudioServicesPlaySystemSound(kSystemSoundID_Vibrate);
        }
    }
}
