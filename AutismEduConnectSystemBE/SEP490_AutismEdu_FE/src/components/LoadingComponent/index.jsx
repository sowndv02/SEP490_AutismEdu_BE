import Backdrop from '@mui/material/Backdrop';
import CircularProgress from '@mui/material/CircularProgress';

export default function LoadingComponent({ open, setOpen }) {
    return (
        <div>
            <Backdrop
                sx={(theme) => ({ color: '#fff', zIndex: theme.zIndex.drawer + 1 })}
                open={open}
            >
                <CircularProgress color="inherit" />
            </Backdrop>
        </div>
    );
}
