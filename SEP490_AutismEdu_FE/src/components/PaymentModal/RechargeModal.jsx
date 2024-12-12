import React, { useEffect, useState } from 'react';
import { Container, Card, CardContent, CardActions, Typography, Button, Grid, Divider } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import QrModal from './QRModal';
import services from '~/plugins/services';
import LoadingComponent from '../LoadingComponent';

const RechargeModal = () => {
    const [loading, setLoading] = useState(false);

    const [servicePackage, setServicePackage] = useState([]);

    const [showQR, setShowQR] = useState(false);
    const [selectedId, setSelectedId] = useState(0);
    const [amount, setAmount] = useState(0);
    const nav = useNavigate();

    useEffect(() => {
        handleGetServicePackage();
    }, []);

    const handleGetServicePackage = async () => {
        try {
            setLoading(true);
            await services.PackagePaymentAPI.getListPaymentPackage((res) => {
                if (res?.result) {
                    setServicePackage(res.result);
                }
            }, (error) => {
                console.log(error);
            }, {});
        } catch (error) {
            console.log(error);

        } finally {
            setLoading(false);
        }
    };

    const handleShowQR = () => setShowQR(true);

    const handleClickUpgrade = (id, price) => {
        setSelectedId(id);
        setAmount(price);
        handleShowQR();
    };


    const generateRandomCode = () => {
        const length = 10;
        const characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        let newCode = "";
        for (let i = 0; i < length; i++) {
            newCode += characters.charAt(Math.floor(Math.random() * characters.length));
        }
        return newCode;
    };

    return (
        <Container maxWidth="lg">
            <Typography variant="h5" textAlign="center" sx={{ mt: 4 }}>
                Nâng cấp tài khoản
            </Typography>
            <Divider sx={{ my: 2 }} />
            <Typography variant="subtitle1" sx={{ color: "#333", mb: 2, fontWeight: 'bold' }}>
                Chọn gói dịch vụ:
            </Typography>
            <Grid container spacing={2}>
                {servicePackage.map((pkg) => (
                    <Grid item xs={12} sm={6} md={3} key={pkg.id}>
                        <Card sx={{ textAlign: 'center', p: 2, height: '100%', display: 'flex', flexDirection: 'column', justifyContent: 'space-between', boxShadow: 3 }}>
                            <CardContent>
                                <Typography variant="h6" fontWeight="bold" color="black">
                                    {pkg.title}
                                </Typography>
                                <Typography variant="body2" color="textSecondary" sx={{ mb: 1 }}>
                                    Thời gian: {pkg.duration} tháng
                                </Typography>
                                <Typography variant="body1" fontWeight="bold">
                                    <span style={{ fontSize: '1.5rem', fontWeight: 'bold' }}>
                                        {pkg.price.toLocaleString('vi-VN')}
                                    </span>
                                    <Typography variant="body2" sx={{ ml: 0.5 }}>
                                        VNĐ
                                    </Typography>
                                </Typography>
                            </CardContent>
                            <CardActions>
                                <Button
                                    fullWidth
                                    variant="contained"
                                    sx={{
                                        bgcolor: "#16ab65",
                                        '&:hover': {
                                            bgcolor: '#128a51',
                                        },
                                    }}
                                    onClick={() => handleClickUpgrade(pkg.id, pkg.price)}
                                >
                                    Nâng cấp
                                </Button>
                            </CardActions>
                        </Card>
                    </Grid>
                ))}
            </Grid>
            
            {showQR && <QrModal show={showQR} setShow={setShowQR} total={amount} randomCode={generateRandomCode()} id={selectedId} />}

            <LoadingComponent open={loading} setOpen={setLoading} />
        </Container>
    );
};

export default RechargeModal;
