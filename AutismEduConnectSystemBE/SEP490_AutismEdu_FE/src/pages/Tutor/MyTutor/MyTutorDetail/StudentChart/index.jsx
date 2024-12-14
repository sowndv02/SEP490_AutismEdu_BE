import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import KeyboardArrowLeftIcon from '@mui/icons-material/KeyboardArrowLeft';
import { Box, Button, Checkbox, FormControl, IconButton, InputLabel, ListItemText, MenuItem, OutlinedInput, Select, Stack, Typography } from "@mui/material";
import {
    CategoryScale,
    Chart as ChartJS,
    Legend,
    LinearScale,
    LineElement,
    PointElement,
    Title,
    Tooltip,
} from "chart.js";
import { useEffect, useState } from "react";
import { Line } from "react-chartjs-2";
import { useParams } from "react-router-dom";
import emptyChart from '~/assets/images/icon/emptychart.gif';
import services from "~/plugins/services";
import PAGES from '~/utils/pages';
ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Title, Tooltip, Legend);

const generatedColors = new Set([
    "rgb(255, 87, 51)",
    "rgb(51, 162, 255)",
    "rgb(141, 255, 51)",
    "rgb(255, 51, 184)",
    "rgb(245, 176, 65)",
    "rgb(93, 109, 126)",
    "rgb(155, 89, 182)",
    "rgb(51, 255, 87)",
    "rgb(255, 189, 51)",
    "rgb(51, 255, 243)",
    "rgb(199, 0, 57)",
    "rgb(255, 87, 51)",
    "rgb(52, 152, 219)",
    "rgb(255, 51, 209)",
    "rgb(175, 122, 197)"
]);

function getUniqueRandomColor() {
    let color;
    do {
        const r = Math.floor(Math.random() * 200);
        const g = Math.floor(Math.random() * 200);
        const b = Math.floor(Math.random() * 200);
        color = `rgb(${r}, ${g}, ${b})`;
    } while (generatedColors.has(color));
    generatedColors.add(color);
    return color;
}

const ITEM_HEIGHT = 48;
const ITEM_PADDING_TOP = 8;
const MenuProps = {
    PaperProps: {
        style: {
            maxHeight: ITEM_HEIGHT * 4.5 + ITEM_PADDING_TOP,
            width: 250
        }
    }
};

function StudentChart({ studentProfile }) {
    const [chartData, setChartData] = useState(null);
    const [loading, setLoading] = useState(false);
    const { id } = useParams();
    const [progressReports, setProgressReports] = useState([]);
    const [assessments, setAssessment] = useState([]);
    const [initialCondition, setInitialCondition] = useState(null);
    const [pagination, setPagination] = useState(null);
    const [displayAssessment, setDisplayAssessment] = useState(null);
    const [selectedAssessment, setSelectedAssessment] = useState([]);
    const [currentPage, setCurrentPage] = useState(1);
    useEffect(() => {
        handleGetReports();
        handleGetAsessment();
    }, []);

    useEffect(() => {
        const reportData = [];
        if (assessments.length !== 0 && progressReports.length !== 0 && studentProfile) {
            assessments.forEach((a) => {
                const data = {
                    label: a.question,
                    data: []
                }
                progressReports.forEach((p) => {
                    const question = p.assessmentResults.find((assessment) => {
                        return assessment.question === a.question;
                    })
                    if (question) {
                        data.data.unshift(question.point)
                    } else {
                        data.data.unshift(null);
                    }
                })
                reportData.unshift(data);
            })
            const colorArray = Array.from(generatedColors);
            const datasets = reportData.map((dataset, index) => ({
                ...dataset,
                borderColor: index < 15 ? colorArray[index] : getUniqueRandomColor(),
                fill: false
            }));
            const label = progressReports.map((p) => {
                return formatDate(p.to);
            }).reverse();
            const labelLength = 10 - label.length;
            for (let i = 0; i <= labelLength; i++) {
                label.push("");
            }
            if ((pagination.total <= 10) || (Math.floor(pagination.total / pagination.pageSize + 1) === pagination.pageNumber)) {
                label.unshift(formatDate(studentProfile.createdDate));
                datasets.forEach((d) => {
                    const initAssessment = initialCondition.find((i) => {
                        return i.question === d.question;
                    })
                    if (initAssessment) {
                        d.data.unshift(initAssessment.point);
                    } else {
                        d.data.unshift(1);
                    }
                })
            }
            setChartData({
                labels: label,
                datasets: datasets
            });
            setDisplayAssessment({
                labels: label,
                datasets: datasets
            })
        }
    }, [assessments, progressReports, studentProfile, pagination, initialCondition])

    useEffect(() => {
        if (selectedAssessment.length !== 0 && displayAssessment?.datasets) {
            const updatedAssessment = chartData.datasets.filter((s) => {
                return selectedAssessment.includes(s.label)
            })
            setDisplayAssessment({
                ...displayAssessment,
                datasets: updatedAssessment
            });
        }
    }, [selectedAssessment])

    useEffect(() => {
        handleGetReports();
    }, [currentPage])
    const handleGetReports = async () => {
        try {
            setLoading(true);
            await services.ProgressReportAPI.getListProgressReport((res) => {
                if (res.result.progressReports.length < 10 && progressReports.length !== 0) {
                    const remainingItem = progressReports.slice(-(10 - res.result.progressReports.length));
                    setProgressReports([...remainingItem, ...res.result.progressReports]);
                } else {
                    setProgressReports(res.result.progressReports);
                }
                setInitialCondition(res.result.initialAssessmentResultDTO);
                setPagination(res.pagination);
            }, (err) => {
                console.log(err);
            }, {
                studentProfileId: id,
                getInitialResult: true,
                orderBy: "dateFrom",
                pageNumber: currentPage
            })
            setLoading(false);
        } catch (error) {
            setLoading(false)
        }
    }

    const formatDate = (date) => {
        if (!date) return ""
        const d = new Date(date);
        return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`
    }
    const handleGetAsessment = async () => {
        try {
            await services.AssessmentManagementAPI.listAssessment((res) => {
                setAssessment(res.result.questions)
                const assessmentNames = res.result.questions.map((r) => {
                    return r.question
                })
                setSelectedAssessment(assessmentNames)
            }, (err) => {
                console.log(err);
            })
        } catch (error) {
            console.log(error);
        }
    }

    const handleChange = (event) => {
        const {
            target: { value },
        } = event;
        setSelectedAssessment(
            typeof value === 'string' ? value.split(',') : value
        );
    };
    return (
        <Box px="150px" pt={2} pb={3}>
            {
                !chartData && (
                    <Stack width="100%" alignItems="center" justifyContent="center" mt="100px">
                        <Box sx={{ textAlign: "center" }}>
                            <img src={emptyChart} style={{ height: "200px" }} />
                            <Typography>Học sinh này chưa có đánh giá nào!!</Typography>
                        </Box>
                    </Stack>
                )
            }
            {
                assessments && assessments.length !== 0 && chartData && (
                    <>
                        <Stack direction='row'>
                            <a href={PAGES.ROOT + PAGES.ASSESSMENT_GUILD_CLIENT} target="_blank"
                                rel="noopener noreferrer">
                                <Button variant='outlined'>Xem cách thức đánh giá ?</Button>
                            </a>
                        </Stack>
                        <Box sx={{ width: "100%", textAlign: "center" }}>
                            <IconButton disabled={pagination?.pageNumber * pagination?.pageSize >= pagination?.total}
                                onClick={() => setCurrentPage(currentPage + 1)}>
                                <KeyboardArrowLeftIcon />
                            </IconButton>
                            <IconButton disabled={currentPage === 1} onClick={() => setCurrentPage(currentPage - 1)}><ChevronRightIcon /></IconButton>
                        </Box>
                    </>
                )
            }
            {
                displayAssessment && displayAssessment.length !== 0 && (
                    <Line
                        data={displayAssessment}
                        options={{
                            plugins: {
                                title: {
                                    display: true,
                                    text: "Biểu đồ tổng quan đánh giá của học sinh"
                                },
                                legend: {
                                    display: true,
                                    position: "bottom"
                                }
                            },
                            scales: {
                                x: {
                                    ticks: {
                                        maxTicksLimit: 11
                                    }
                                },
                                y: {
                                    suggestedMin: 1,
                                    suggestedMax: 4,
                                    beginAtZero: false,
                                    ticks: {
                                        stepSize: 0.5,
                                        callback: function (value) {
                                            return value.toFixed(1);
                                        }
                                    }
                                }
                            }
                        }}
                    />
                )
            }
        </Box>
    );
}

export default StudentChart
